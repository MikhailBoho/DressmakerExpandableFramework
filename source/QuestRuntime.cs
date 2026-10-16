using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dressmaker.DeskMail
{
    // All reflection is bound once, on Unity's main thread. No game assembly is
    // redistributed. Definitions enter the loader's deterministic save registry.
    internal sealed class QuestRuntime
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Type questType = Reflect.Type("QuestDefinition");
        private readonly Type clientType = Reflect.Type("CharacterDefinition");
        private readonly Type progressType = Reflect.Type("PlayerProgress");
        private readonly Type stateType = Reflect.Type("QuestState");
        private readonly Type managerType = Reflect.Type("GameManager");
        private readonly Dictionary<string, object> definitions = new Dictionary<string, object>();
        private readonly Dictionary<string, object> clients = new Dictionary<string, object>();
        private readonly DeliveryGate deliveries = new DeliveryGate();
        private readonly GameBridge bridge;
        private readonly Action<string> log;
        private readonly FieldInfo active, completed, stateDefinition, outcome, proposed, submitted, forceOutcome, activeDress;
        private readonly PropertyInfo current, hasSave, currentState, pendingUnlocks;
        private readonly MethodInfo register, resolve, createRule, measure, design, switchCommission, save, complete, satisfy;
        private readonly Type ruleType;
        private readonly EmbeddedArtwork portrait;
        private object observedProgress;
        private object lastConfirmationPaper;
        private int lastConfirmationFrame = -1;
        private bool presentingReceipt;
        private object deliveryState;
        public readonly MethodInfo PlayDebriefing, ConfirmMeasurement;

        public QuestRuntime(GameBridge bridge, Action<string> log)
        {
            this.bridge = bridge; this.log = log;
            var registry = Reflect.Type("DressmakerMods.StoryAssetRegistry");
            register = StaticMethod(registry, "Register", Reflect.Type("Framework.ScriptableEnum"), typeof(string));
            foreach (var method in registry.GetMethods(Static))
                if (method.Name == "Resolve" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1)
                    resolve = method;
            if (resolve == null) throw new MissingMethodException(registry.FullName, "Resolve<T>(string)");
            ruleType = Reflect.Type("DressmakerMods.ModQuestRequirement");
            createRule = StaticMethod(Reflect.Type("DressmakerMods.StoryRequirements"), "Create", ruleType, typeof(string));
            current = StaticProperty(progressType, "Current");
            hasSave = StaticProperty(progressType, "HasSaveLoaded");
            currentState = Reflect.Property(progressType, "currentQuestState");
            active = Reflect.Field(progressType, "activeCommissions");
            completed = Reflect.Field(progressType, "completedQuests");
            stateDefinition = Reflect.Field(stateType, "questDefinition");
            outcome = Reflect.Field(stateType, "outcome");
            proposed = Reflect.Field(managerType, "proposedQuest");
            submitted = Reflect.Field(managerType, "commissionSubmitted");
            forceOutcome = Reflect.Field(managerType, "commissionForceOutcome");
            activeDress = Reflect.Field(managerType, "activeDress");
            pendingUnlocks = Reflect.Property(managerType, "HasPendingUnlocks");
            measure = Reflect.Method(managerType, "SetSceneMeasuring");
            design = Reflect.Method(managerType, "SetSceneSketchbook");
            switchCommission = Reflect.Method(progressType, "SwitchActiveCommission", typeof(int));
            save = StaticMethod(progressType, "Save", progressType);
            complete = Reflect.Method(Reflect.Type("FrontDesk"), "CompleteQuest");
            satisfy = Reflect.Method(Reflect.Type("Dress"), "DoesDressSatisfyCommission", questType);
            PlayDebriefing = Reflect.Method(Reflect.Type("FrontDesk"), "PlayDebriefing");
            ConfirmMeasurement = Reflect.Method(Reflect.Type("MeasurePaper"), "ConfirmBodyMeasurement");
            if (PlayDebriefing.ReturnType != typeof(IEnumerator) || complete.ReturnType != typeof(void) || satisfy.ReturnType != typeof(bool))
                throw new InvalidOperationException("The native commission API differs from the supported game build.");

            portrait = EmbeddedArtwork.Load("mail-holder.png");
            foreach (var quest in SampleQuests.All) RegisterQuest(quest);
            log("Registered " + definitions.Count + " mail commissions and " + clients.Count + " correspondents before save loading.");
        }

        private static MethodInfo StaticMethod(Type type, string name, params Type[] args) =>
            type.GetMethod(name, Static, null, args, null) ?? throw new MissingMethodException(type.FullName, name);
        private static PropertyInfo StaticProperty(Type type, string name) =>
            type.GetProperty(name, Static) ?? throw new MissingMemberException(type.FullName, name);
        private object Resolve(Type type, string name) => resolve.MakeGenericMethod(type).Invoke(null, new object[] { name });
        private static void SetField(object obj, string name, object value) => Reflect.Field(obj.GetType(), name).SetValue(obj, value);
        private static void EmptyField(object obj, string name)
        {
            var field = Reflect.Field(obj.GetType(), name);
            field.SetValue(obj, field.FieldType.IsArray ? Array.CreateInstance(field.FieldType.GetElementType(), 0) : Activator.CreateInstance(field.FieldType));
        }

        private void RegisterQuest(SampleQuest spec)
        {
            if (!clients.TryGetValue(spec.ClientId, out var client))
            {
                var clientName = SampleQuests.Namespace + ".client." + spec.ClientId;
                client = Resolve(clientType, clientName);
                if (!Reflect.Alive(client))
                {
                    // Retain the game's generic measuring silhouette, never a named
                    // character's face. Mail artwork serves as the small client icon.
                    var template = StaticProperty(clientType, "Alice").GetValue(null, null) as UnityEngine.Object;
                    if (!template) throw new InvalidOperationException("The native measuring silhouette is not loaded.");
                    client = UnityEngine.Object.Instantiate(template);
                    Reflect.Set(client, "PrettyName", spec.Sender);
                    SetField(client, "giftQuest", null);
                    EmptyField(client, "levelUnlocks");
                    EmptyField(client, "specialPortraits");
                    foreach (var field in new[] { "sprite", "happySprite", "sadSprite", "postcardSprite" }) SetField(client, field, portrait.Sprite);
                    SetField(client, "rolodexPortraitPosition", Vector2.zero);
                    SetField(client, "popupPortraitPosition", Vector2.zero);
                    SetField(client, "rolodexPortraitScale", 1f);
                    SetField(client, "popupPortraitScale", 1f);
                    var shape = Activator.CreateInstance(Reflect.Type("Measurements"));
                    bool juniper = spec.ClientId == "juniper";
                    SetField(shape, "bust", juniper ? 0.22f : 0.48f);
                    SetField(shape, "waist", juniper ? 0.28f : 0.43f);
                    SetField(shape, "hips", juniper ? 0.24f : 0.46f);
                    SetField(client, "measurements", shape);
                    register.Invoke(null, new[] { client, clientName });
                }
                clients.Add(spec.ClientId, client);
            }
            var definition = Resolve(questType, spec.RegistryName);
            if (!Reflect.Alive(definition))
            {
                definition = ScriptableObject.CreateInstance(questType);
                SetField(definition, "prettyName", spec.Title);
                SetField(definition, "questGiverCharacter", client);
                SetField(definition, "budget", spec.Payment);
                EmptyField(definition, "requirements");
                var rulesField = Reflect.Field(questType, "completionRequirements");
                var rules = (IList)Activator.CreateInstance(rulesField.FieldType);
                foreach (var specRule in spec.Rules)
                {
                    var rule = Activator.CreateInstance(ruleType);
                    Reflect.Set(rule, "Type", specRule.Type);
                    Reflect.Set(rule, "Value", specRule.Value);
                    Reflect.Set(rule, "Comparison", "AtLeast");
                    if (specRule.Color != null) Reflect.Set(rule, "Color", specRule.Color);
                    rules.Add(createRule.Invoke(null, new[] { rule, SampleQuests.Namespace }));
                }
                rulesField.SetValue(definition, rules);
                register.Invoke(null, new[] { definition, spec.RegistryName });
            }
            definitions.Add(spec.LetterId, definition);
        }

        public object Progress => (bool)hasSave.GetValue(null, null) ? current.GetValue(null, null) : null;
        private object FindState(object progress, FieldInfo listField, string id)
        {
            if (progress == null || !definitions.TryGetValue(id, out var definition)) return null;
            foreach (var state in (IList)listField.GetValue(progress))
                if (state != null && ReferenceEquals(stateDefinition.GetValue(state), definition)) return state;
            return null;
        }
        private bool Succeeded(object progress, string id)
        {
            var state = FindState(progress, completed, id);
            if (state == null) return false;
            var value = outcome.GetValue(state).ToString();
            return value == "Succeeded" || value == "Excelled";
        }
        public CommissionStatus Status(string id)
        {
            var progress = Progress;
            return SampleQuests.Status(SampleQuests.Find(id), progress != null,
                FindState(progress, active, id) != null, FindState(progress, completed, id) != null,
                prerequisite => Succeeded(progress, prerequisite));
        }
        public bool Refresh(MailSession session)
        {
            var progress = Progress;
            bool changedSave = !ReferenceEquals(observedProgress, progress);
            if (changedSave)
            {
                observedProgress = progress; CancelPresentation(); deliveries.Reset(); session.ResetReading();
                lastConfirmationPaper = null; lastConfirmationFrame = -1;
                if (bridge.Manager)
                {
                    var pending = proposed.GetValue(bridge.Manager);
                    foreach (var definition in definitions.Values)
                        if (ReferenceEquals(pending, definition)) { proposed.SetValue(bridge.Manager, null); break; }
                }
            }
            for (int i = 0; i < MailCatalog.Letters.Count; i++)
            {
                var spec = SampleQuests.Find(MailCatalog.Letters[i].Id);
                session.SetAvailable(i, spec == null || spec.After == null || Status(spec.LetterId) != CommissionStatus.Unavailable);
            }
            return changedSave;
        }

        public void Act(string id)
        {
            var status = Status(id);
            if (presentingReceipt) throw new InvalidOperationException("Close the delivery letter before beginning another commission.");
            if (!bridge.CanOpen(out var reason)) throw new InvalidOperationException(reason);
            if ((bool)pendingUnlocks.GetValue(bridge.Manager, null)) throw new InvalidOperationException("Finish the game's unlock notices first.");
            if (status == CommissionStatus.Available)
            {
                var pending = proposed.GetValue(bridge.Manager);
                if (Reflect.Alive(pending)) throw new InvalidOperationException("Finish or decline the proposed commission first.");
                var definition = definitions[id];
                proposed.SetValue(bridge.Manager, definition);
                try
                {
                    measure.Invoke(bridge.Manager, null);
                    // Confirmation remains entirely native: it records true measured
                    // centimetres, preserves the old dress, appends the quest and saves.
                    log("Measuring mail client for " + id + "; acceptance awaits confirmation.");
                }
                catch
                {
                    if (ReferenceEquals(proposed.GetValue(bridge.Manager), definition)) proposed.SetValue(bridge.Manager, null);
                    throw;
                }
            }
            else if (status == CommissionStatus.Active)
            {
                var progress = Progress;
                var state = FindState(progress, active, id);
                var index = ((IList)active.GetValue(progress)).IndexOf(state);
                switchCommission.Invoke(progress, new object[] { index });
                save.Invoke(null, new object[] { null });
                design.Invoke(bridge.Manager, null);
                log("Resumed mail commission " + id + ".");
            }
        }

        public bool AllowMeasurementConfirmation(object paper)
        {
            if (!bridge.Manager || (bool)Reflect.Field(paper.GetType(), "IsMannequinScene").GetValue(paper)) return true;
            var definition = proposed.GetValue(bridge.Manager);
            if (!Reflect.Alive(definition) && ReferenceEquals(paper, lastConfirmationPaper) && Time.frameCount == lastConfirmationFrame) return false;
            foreach (var pair in definitions)
                if (ReferenceEquals(pair.Value, definition))
                {
                    if (Status(pair.Key) != CommissionStatus.Available) return false;
                    lastConfirmationPaper = paper; lastConfirmationFrame = Time.frameCount;
                    return true;
                }
            return true;
        }

        public bool IsMailDelivery(out SampleQuest spec, out object state)
        {
            spec = null; state = null;
            if (!bridge.Manager || Progress == null || !(bool)submitted.GetValue(bridge.Manager)) return false;
            state = currentState.GetValue(Progress, null);
            if (state == null) return false;
            var definition = stateDefinition.GetValue(state);
            foreach (var candidate in SampleQuests.All)
                if (ReferenceEquals(definitions[candidate.LetterId], definition)) { spec = candidate; return true; }
            return false;
        }

        public void CancelPresentation()
        {
            // Unity may stop the desk coroutine on scene teardown. Release its
            // transient ownership explicitly; any reward-commit guard survives.
            presentingReceipt = false;
            if (deliveryState != null) deliveries.End(deliveryState);
            deliveryState = null;
        }

        // No Yarn node, customer sprite, or forced-outcome cheat is used. A native
        // success archives and pays; an unmet rule leaves the same work active.
        public IEnumerator Deliver(object desk, SampleQuest spec, object state,
            Action<LetterDefinition> show, Func<bool> mailOpen, Action<Exception> error)
        {
            submitted.SetValue(bridge.Manager, false);
            forceOutcome.SetValue(bridge.Manager, null);
            if (!deliveries.TryBegin(state)) yield break;
            presentingReceipt = true;
            deliveryState = state;
            try
            {
                LetterDefinition response = null;
                try
                {
                    if (!ReferenceEquals(currentState.GetValue(Progress, null), state) ||
                        !ReferenceEquals(FindState(Progress, active, spec.LetterId), state))
                        throw new InvalidOperationException("The current commission changed before mail delivery.");
                    if (outcome.GetValue(state).ToString() != "NotCompleted")
                        throw new InvalidOperationException("This delivery already began completion; it will not pay twice. Please share LogOutput.log.");
                    var dress = activeDress.GetValue(bridge.Manager);
                    if (!Reflect.Alive(dress)) throw new InvalidOperationException("No completed dress is available to deliver.");
                    bool met = (bool)satisfy.Invoke(dress, new[] { definitions[spec.LetterId] });
                    var missing = met ? null : UnmetRules(dress, definitions[spec.LetterId], spec);
                    if (met)
                    {
                        // Do not release this guard on an exception in a native reward
                        // callback. Automatically retrying could grant money twice.
                        deliveries.BeginRewards(state);
                        outcome.SetValue(state, Enum.Parse(outcome.FieldType, "Succeeded"));
                        complete.Invoke(desk, null);
                        log("Delivered " + spec.LetterId + ": native success, payment " + spec.Payment + ".");
                    }
                    else log("Adjustments requested for " + spec.LetterId + "; dress, payment and progression unchanged.");
                    var letter = FindLetter(spec.LetterId);
                    response = new LetterDefinition(spec.LetterId + ".response", spec.Sender, letter.Role, letter.Archetype,
                        met ? "A letter of thanks" : "A small alteration, if you please", "Delivery reply", letter.Stationery,
                        met ? spec.Thanks + "\n\nPayment received: " + spec.Payment + " coins." :
                        "Dear dressmaker,\n\nThank you for the care you have taken. Before our arrangement is complete, could we attend to the following?\n\n" + missing +
                        "\nThe dress remains with you, and our commission stays open. Please adjust it and send it through the usual delivery controls when ready. Payment will follow once our agreed requirements are met.\n\nWith warm regards,\n" + spec.Sender);
                }
                catch (Exception failure) { error(failure); }
                if (response != null)
                {
                    try { show(response); } catch (Exception failure) { error(failure); }
                    while (ReferenceEquals(deliveryState, state) && mailOpen() && bridge.DeskAlive) yield return null;
                }
            }
            finally
            {
                deliveries.End(state);
                if (ReferenceEquals(deliveryState, state)) CancelPresentation();
            }
        }

        private string UnmetRules(object dress, object definition, SampleQuest spec)
        {
            var lines = new List<string>();
            var rules = (IList)Reflect.Field(questType, "completionRequirements").GetValue(definition);
            for (int i = 0; i < rules.Count; i++)
                if (!(bool)Reflect.Method(rules[i].GetType(), "IsMet", dress.GetType(), questType).Invoke(rules[i], new[] { dress, definition }))
                    lines.Add("- " + spec.Rules[i].Description + ".");
            return string.Join("\n", lines) + "\n";
        }
        private static LetterDefinition FindLetter(string id)
        {
            foreach (var letter in MailCatalog.Letters) if (letter.Id == id) return letter;
            throw new InvalidOperationException("Missing mail for " + id);
        }
    }
}
