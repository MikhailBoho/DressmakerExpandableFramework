using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dressmaker.DeskMail
{
    internal static class Reflect
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> properties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        public const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static Type Type(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            throw new MissingMemberException("Required runtime type: " + name);
        }
        public static FieldInfo Field(Type type, string name) =>
            type.GetField(name, Instance) ?? throw new MissingFieldException(type.FullName, name);
        public static PropertyInfo Property(Type type, string name)
        {
            // All access is on Unity's main thread. Cache the UI properties used each frame.
            if (!properties.TryGetValue(type, out var members))
                properties[type] = members = new Dictionary<string, PropertyInfo>();
            if (!members.TryGetValue(name, out var property))
                members[name] = property = type.GetProperty(name, Instance) ?? throw new MissingMemberException(type.FullName, name);
            return property;
        }
        public static MethodInfo Method(Type type, string name, params Type[] args) =>
            type.GetMethod(name, Instance, null, args, null) ?? throw new MissingMethodException(type.FullName, name);
        public static object Get(object obj, string property) => Property(obj.GetType(), property).GetValue(obj, null);
        public static void Set(object obj, string property, object value) => Property(obj.GetType(), property).SetValue(obj, value, null);
        public static void SetEnum(object obj, string property, int value)
        {
            var p = Property(obj.GetType(), property);
            p.SetValue(obj, Enum.ToObject(p.PropertyType, value), null);
        }
        public static void SetEnum(object obj, string property, string value)
        {
            var p = Property(obj.GetType(), property);
            p.SetValue(obj, Enum.Parse(p.PropertyType, value), null);
        }
        public static bool Alive(object value) => value is UnityEngine.Object native ? native != null : value != null;
        public static bool SceneObject(Component value) => value && value.gameObject.scene.IsValid();
        public static Component FindSceneComponent(Type type)
        {
            // Includes inactive presentation objects, but excludes prefab/asset objects.
            foreach (var obj in Resources.FindObjectsOfTypeAll(type))
                if (obj is Component component && SceneObject(component)) return component;
            return null;
        }
    }

    internal sealed class GameBridge
    {
        private readonly Type managerType = Reflect.Type("GameManager");
        private readonly Type deskType = Reflect.Type("FrontDesk");
        private readonly Type letterType = Reflect.Type("KnighthoodScene");
        private readonly Type pigeonType = Reflect.Type("MessengerPigeonUI");
        private readonly FieldInfo deskField, introField, submittedField, blockerField, optionsField, bellField;
        private readonly FieldInfo dialogueField, nativeBackground, nativeEnvelope, nativePaper, nativePage;
        private readonly PropertyInfo presenting, nativeOpen, optionsOpen;
        public readonly MethodInfo NextPresentation, SetScene, ShowHideOptions, NativeShow, PigeonShow, DeskDisabled;
        public Component Manager { get; private set; }
        public Behaviour Desk { get; private set; }
        public Component Letter { get; private set; }
        public Component Pigeon { get; private set; }
        public Component Bell => Desk ? bellField.GetValue(Desk) as Component : null;

        public GameBridge()
        {
            deskField = Reflect.Field(managerType, "frontDesk");
            introField = Reflect.Field(managerType, "isInIntro");
            submittedField = Reflect.Field(managerType, "commissionSubmitted");
            blockerField = Reflect.Field(managerType, "progressDisplayInputBlocker");
            optionsField = Reflect.Field(managerType, "optionsMenu");
            bellField = Reflect.Field(deskType, "bellButton");
            dialogueField = Reflect.Field(deskType, "dialogueText");
            presenting = Reflect.Property(deskType, "IsPresenting");
            nativeOpen = Reflect.Property(letterType, "IsOpen");
            optionsOpen = Reflect.Property(optionsField.FieldType, "IsShowing");
            nativeBackground = Reflect.Field(letterType, "background");
            nativeEnvelope = Reflect.Field(letterType, "envelope");
            nativePaper = Reflect.Field(letterType, "letter");
            nativePage = Reflect.Field(letterType, "page1");
            NextPresentation = Reflect.Method(deskType, "NextPresentation");
            if (NextPresentation.ReturnType != typeof(IEnumerator))
                throw new MissingMethodException("FrontDesk.NextPresentation must return IEnumerator.");
            SetScene = SceneTransitionBinding.Resolve(managerType);
            ShowHideOptions = Reflect.Method(managerType, "ShowHideOptions");
            NativeShow = Reflect.Method(letterType, "ShowKnighthoodEnvelope");
            PigeonShow = Reflect.Method(pigeonType, "Show");
            DeskDisabled = Reflect.Method(deskType, "OnDisable");
        }

        // Called periodically; never scan all game objects per frame.
        public void Refresh()
        {
            if (!Reflect.SceneObject(Manager)) Manager = Reflect.FindSceneComponent(managerType);
            Desk = Manager ? deskField.GetValue(Manager) as Behaviour : null;
            if (!Reflect.SceneObject(Letter)) Letter = Reflect.FindSceneComponent(letterType);
            if (!Reflect.SceneObject(Pigeon)) Pigeon = Reflect.FindSceneComponent(pigeonType);
        }

        public bool DeskAlive => Desk && Desk.isActiveAndEnabled;

        // The holder is a desk prop even when dialogue or a modal blocks input.
        // Follow the desk GameObject, not the controller's enabled/input state.
        public bool DeskVisible => Desk && Desk.gameObject.activeInHierarchy;

        public bool CanOpen(out string reason)
        {
            if (!Manager || !DeskAlive) { reason = "Visit the front desk."; return false; }
            if ((bool)introField.GetValue(Manager)) { reason = "Finish opening the shop first."; return false; }
            if ((bool)presenting.GetValue(Desk) || (bool)submittedField.GetValue(Manager))
            { reason = "Wait for the current customer or delivery to finish."; return false; }
            var options = optionsField.GetValue(Manager);
            if (Reflect.Alive(options) && (bool)optionsOpen.GetValue(options, null))
            { reason = "Close the pause menu first."; return false; }
            var blocker = blockerField.GetValue(Manager) as GameObject;
            if (blocker && blocker.activeInHierarchy)
            { reason = "Finish the progress notification first."; return false; }
            if (Time.timeScale <= 0f) { reason = "Resume the game first."; return false; }
            if (Pigeon && Pigeon.gameObject.activeInHierarchy)
            { reason = "Close the visiting-card recipient list first."; return false; }
            if (Letter)
            {
                var background = nativeBackground.GetValue(Letter) as Component;
                if ((bool)nativeOpen.GetValue(Letter, null) || (background && background.gameObject.activeInHierarchy))
                { reason = "Finish reading the original letter first."; return false; }
            }
            if (!Bell || !Bell.gameObject.activeInHierarchy)
            { reason = "The desk controls are not ready."; return false; }
            reason = "Ready";
            return true;
        }

        public object Handwriting(Type tmpType)
        {
            if (Letter)
            {
                var page = nativePage.GetValue(Letter) as GameObject;
                var text = page ? page.GetComponent(tmpType) : null;
                if (text)
                {
                    var font = Reflect.Get(text, "font");
                    if (Reflect.Alive(font)) return font;
                }
            }
            var fallback = Desk ? dialogueField.GetValue(Desk) as Component : null;
            return fallback ? Reflect.Get(fallback, "font") : null;
        }

        public Sprite EnvelopeSprite => GetSprite(nativeEnvelope);
        public Sprite PaperSprite => GetSprite(nativePaper);
        private Sprite GetSprite(FieldInfo field)
        {
            var graphic = Letter ? field.GetValue(Letter) as Component : null;
            return graphic ? Reflect.Get(graphic, "sprite") as Sprite : null;
        }
        public IEnumerator ResumePresentation(object desk) => (IEnumerator)NextPresentation.Invoke(desk, null);
    }
}
