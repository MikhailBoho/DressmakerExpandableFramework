using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Dressmaker.DeskMail
{
    [BepInPlugin(Id, "Dressmaker Desk Mail Prototype", Version)]
    [BepInDependency("yuu.dressmaker.mods", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class DeskMailPlugin : BaseUnityPlugin
    {
        public const string Id = "community.dressmaker.deskmailprototype";
        public const string Version = "0.2.0";
        private static DeskMailPlugin instance;
        private readonly MailSession session = new MailSession();
        private GameBridge bridge;
        private MailUi ui;
        private Harmony harmony;
        private Harmony questHarmony;
        private QuestRuntime quests;
        private ConfigEntry<bool> enabledSetting, pulseUnread, animateOpening;
        private ConfigEntry<KeyboardShortcut> openKey;
        private ConfigEntry<PaperSelection> paperSelection;
        private ConfigEntry<float> offsetX, offsetY, width;
        private float nextProbe;
        private bool failed;
        private string previousStatus;

        private void Awake()
        {
            instance = this;
            enabledSetting = Config.Bind("General", "Enabled", true, "Show the desk mail inbox and offer sample commissions. Saved commission definitions and delivery support remain loaded when this is false.");
            openKey = Config.Bind("Controls", "OpenKey", new KeyboardShortcut(KeyCode.F8), "Open/close the letter inbox while the desk is idle.");
            pulseUnread = Config.Bind("Desk mail", "PulseUnread", true, "Gently pulse the holder red while mail is unread.");
            animateOpening = Config.Bind("Desk mail", "AnimateOpening", true, "Open each letter with a brief envelope and paper-unfold animation. False shows the letter immediately.");
            paperSelection = Config.Bind("Desk mail", "Stationery", PaperSelection.BySender,
                "BySender uses each correspondent's paper. Celestial, Botanical, or Rose overrides it; Classic uses the original game paper.");
            // Preserve the old section name so existing position settings continue to work.
            offsetX = Config.Bind("Desk envelope", "OffsetX", HolderLayout.DefaultX,
                new ConfigDescription("Horizontal offset from the bell. The holder is kept at least 16 canvas units clear of the bell's left edge.", new AcceptableValueRange<float>(-1500, 1500)));
            offsetY = Config.Bind("Desk envelope", "OffsetY", -90f,
                new ConfigDescription("Vertical offset from the bell. Negative is below.", new AcceptableValueRange<float>(-1000, 1000)));
            width = Config.Bind("Desk envelope", "Width", HolderLayout.DefaultWidth,
                new ConfigDescription("Mail holder width in the desk canvas's units.", new AcceptableValueRange<float>(60, 400)));
            var layoutRevision = Config.Bind("Desk mail", "LayoutRevision", 0, "Tracks the one-time holder layout upgrade. Leave this value unchanged.");
            if (layoutRevision.Value < HolderLayout.Revision)
            {
                float migratedX = offsetX.Value, migratedWidth = width.Value;
                HolderLayout.Migrate(layoutRevision.Value, ref migratedX, ref migratedWidth);
                offsetX.Value = migratedX; width.Value = migratedWidth;
                layoutRevision.Value = HolderLayout.Revision;
            }
            try
            {
                bridge = new GameBridge();
                // Always register definitions, even with the inbox disabled: saves
                // must resolve their stable custom quest and client identities.
                quests = new QuestRuntime(bridge, message => Logger.LogInfo(message));
                questHarmony = new Harmony(Id + ".commissions");
                questHarmony.Patch(quests.PlayDebriefing, prefix: new HarmonyMethod(typeof(DeskMailPlugin), nameof(DeliverMail)));
                questHarmony.Patch(quests.ConfirmMeasurement, prefix: new HarmonyMethod(typeof(DeskMailPlugin), nameof(ConfirmMailMeasurement)));
                Logger.LogInfo("Scene transition binding: " + bridge.SetScene.DeclaringType.FullName + "." + bridge.SetScene.Name + "(" + bridge.SetScene.GetParameters()[0].ParameterType.FullName + ", Boolean).");
                harmony = new Harmony(Id);
                Patch(bridge.NextPresentation, nameof(DeferPresentation));
                Patch(bridge.SetScene, nameof(SceneChanging));
                Patch(bridge.ShowHideOptions, nameof(AllowOptions));
                Patch(bridge.NativeShow, nameof(NativeLetterOpening));
                Patch(bridge.PigeonShow, nameof(NativeLetterOpening));
                Patch(bridge.DeskDisabled, nameof(DeskDisabling));
                Logger.LogInfo("Desk Mail " + Version + " ready. Three playable mail commissions; native measurement, scoring, payment and saves. Click the mail holder at an idle desk, or press F8.");
            }
            catch (Exception error) { Fail(error); }
        }

        private void Patch(System.Reflection.MethodInfo original, string prefix) =>
            harmony.Patch(original, prefix: new HarmonyMethod(typeof(DeskMailPlugin), prefix));

        private void Update()
        {
            if ((failed && !session.IsNotice) || bridge == null) return;
            try
            {
                if (!enabledSetting.Value && !session.IsNotice) { Close(false); ui?.UpdateHolder(false, false, false, 0, 0, HolderLayout.DefaultWidth, false); }
                if (Time.unscaledTime >= nextProbe)
                {
                    nextProbe = Time.unscaledTime + 1f;
                    bridge.Refresh();
                    if (quests.Refresh(session)) Close(false);
                    if (bridge.DeskVisible && enabledSetting.Value)
                    {
                        EnsureUi();
                        ui.EnsureHolder(bridge);
                    }
                }
                if (session.IsOpen && !bridge.DeskAlive) Close(false);
                if (session.IsOpen)
                {
                    if (openKey.Value.IsDown()) Close(true);
                    else if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        if (session.IsReading) BackToInbox();
                        else Close(true);
                    }
                    else if (Input.GetKeyDown(KeyCode.PageDown)) Turn(1);
                    else if (Input.GetKeyDown(KeyCode.PageUp)) Turn(-1);
                }
                else if (openKey.Value.IsDown()) Open();
            }
            catch (Exception error) { Fail(error); }
        }

        private void LateUpdate()
        {
            if ((failed && !session.IsNotice) || bridge == null || ui == null) return;
            try
            {
                var available = bridge.CanOpen(out _);
                ui.UpdateHolder(bridge.DeskVisible && enabledSetting.Value, available && !session.IsOpen, session.HasUnread,
                    offsetX.Value, offsetY.Value, width.Value, pulseUnread.Value);
                if (session.IsOpen) { ui.Layout(); ui.Animate(Time.unscaledDeltaTime); }
            }
            catch (Exception error) { Fail(error); }
        }

        private void Open()
        {
            try
            {
                if (failed || bridge == null || !enabledSetting.Value) return;
                bridge.Refresh();
                var available = bridge.CanOpen(out var reason);
                if (!available || ui == null)
                {
                    var status = ui == null && available ? "The desk UI is still loading; try again in a moment." : reason;
                    if (status != previousStatus) { Logger.LogInfo("Mail unavailable: " + status); previousStatus = status; }
                    return;
                }
                if (!session.Open(true, Time.frameCount)) return;
                quests.Refresh(session);
                ui.UpdateAssets(bridge);
                ui.ShowInbox(true);
                Logger.LogInfo("Opened inbox: " + session.UnreadCount + " unread letters.");
            }
            catch (Exception error) { Fail(error); }
        }

        private void Turn(int direction)
        {
            try
            {
                if (!session.IsReading) return;
                ui.FinishOpening();
                session.Turn(direction);
                ui.SetPage();
            }
            catch (Exception error) { Fail(error); }
        }

        private void SelectLetter(int index)
        {
            try
            {
                if (!session.SelectLetter(index)) return;
                ui.ShowLetter(paperSelection.Value, animateOpening.Value);
                Logger.LogInfo("Opened letter: " + session.CurrentLetter.Id + ".");
            }
            catch (Exception error) { Fail(error); }
        }

        private void BackToInbox()
        {
            try
            {
                if (session.IsNotice) { Close(true); return; }
                if (session.BackToInbox()) ui.ShowInbox(false);
            }
            catch (Exception error) { Fail(error); }
        }

        private void EnsureUi()
        {
            if (ui == null)
                ui = new MailUi(transform, bridge, session, Open, () => Close(true), BackToInbox, SelectLetter, Turn,
                    CommissionAction, id => quests.Status(id));
        }

        private void CommissionAction()
        {
            if (!session.IsReading || session.IsNotice) return;
            try
            {
                string id = session.CurrentLetter.Id;
                var status = quests.Status(id);
                if (status == CommissionStatus.Available && session.Page + 1 < session.CurrentLetter.Pages.Count)
                { Turn(session.CurrentLetter.Pages.Count); return; }
                if (status != CommissionStatus.Available && status != CommissionStatus.Active) return;
                Close(true);
                quests.Act(id);
            }
            catch (Exception error) { QuestError(error); }
        }

        private void ShowReply(LetterDefinition response)
        {
            bridge.Refresh();
            EnsureUi();
            quests.Refresh(session);
            session.ShowNotice(response, Time.frameCount);
            ui.UpdateAssets(bridge);
            ui.ShowLetter(paperSelection.Value, !failed && animateOpening.Value);
        }

        private void QuestError(Exception error)
        {
            Logger.LogError("Mail commission action stopped: " + error);
            try
            {
                ShowReply(new LetterDefinition("deskmail.problem", "The correspondence desk", "", "",
                    "An interruption", "", StationeryProfile.Rose,
                    "This commission action could not finish. Please close this letter and check BepInEx/LogOutput.log before trying again.\n\n" +
                    (error.InnerException ?? error).Message));
            }
            catch (Exception displayError) { Logger.LogError(displayError); CloseSafely(false); }
        }

        private static bool ConfirmMailMeasurement(object __instance)
        {
            var plugin = instance;
            if (plugin?.quests == null) return true;
            try { return plugin.quests.AllowMeasurementConfirmation(__instance); }
            catch (Exception error) { plugin.QuestError(error); return false; }
        }

        private static bool DeliverMail(object __instance, ref IEnumerator __result)
        {
            var plugin = instance;
            if (plugin?.quests == null) return true;
            if (!plugin.quests.IsMailDelivery(out var spec, out var state)) return true;
            __result = plugin.quests.Deliver(__instance, spec, state, plugin.ShowReply,
                () => plugin && plugin.session.IsOpen, plugin.QuestError);
            return false;
        }

        private void Close(bool read)
        {
            if (!session.IsOpen) return;
            session.Close(read, Time.frameCount);
            ui?.Hide();
            Logger.LogInfo(read ? "Mail closed; " + session.UnreadCount + " unread letters remain." : "Mail dismissed for a game transition.");
        }

        private static bool DeferPresentation(object __instance, ref IEnumerator __result)
        {
            var plugin = instance;
            if (plugin == null || !plugin.session.IsOpen || !ReferenceEquals(plugin.bridge.Desk, __instance)) return true;
            var desk = __instance as Behaviour;
            __result = PresentationDeferral.Wait(() => plugin && plugin.session.IsOpen,
                () => desk && desk.isActiveAndEnabled, () => plugin.bridge.ResumePresentation(__instance));
            return false;
        }

        private static void SceneChanging()
        {
            instance?.quests?.CancelPresentation();
            instance?.CloseSafely(false);
        }
        private static void NativeLetterOpening() => instance?.CloseSafely(false);
        private static void DeskDisabling(object __instance)
        {
            if (instance && ReferenceEquals(instance.bridge?.Desk, __instance))
            {
                instance.quests?.CancelPresentation();
                instance.CloseSafely(false);
            }
        }
        private static bool AllowOptions() => !instance || !instance.session.BlocksOptions(Time.frameCount);
        private void CloseSafely(bool read)
        {
            try { Close(read); } catch (Exception error) { Fail(error); }
        }

        private void Fail(Exception error)
        {
            failed = true;
            quests?.CancelPresentation();
            session.Close(false, Time.frameCount);
            try { ui?.Dispose(); } catch { /* Continue to release our patches even if UI teardown fails. */ }
            ui = null;
            try { harmony?.UnpatchSelf(); } catch (Exception unpatchError) { Logger.LogError(unpatchError); }
            Logger.LogError("Desk Mail disabled itself after an error. Native gameplay remains available. Please share this log entry: " + error);
        }

        private void OnDestroy()
        {
            session.Close(false, Time.frameCount);
            try { ui?.Dispose(); }
            finally
            {
                harmony?.UnpatchSelf();
                questHarmony?.UnpatchSelf();
                if (ReferenceEquals(instance, this)) instance = null;
            }
        }
    }
}
