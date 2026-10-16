using System;
using System.Collections;
using System.Collections.Generic;
using Dressmaker.DeskMail;

internal static class StateTests
{
    private static int passed;
    private static void Check(bool value, string reason)
    {
        if (!value) throw new Exception(reason);
    }
    private static void Test(string name, Action body)
    {
        body(); passed++; Console.WriteLine("PASS " + name);
    }
    private static IEnumerator Presentation() { yield return "native presentation"; }
    private sealed class NestedSceneManager
    {
        public enum Scene { Desk, Workroom }
        public Scene CurrentScene { get; private set; }
        public void SetScene(Scene scene, bool instant) { CurrentScene = scene; }
        public void SetScene(int scene, bool instant) { throw new Exception("Bound the wrong overload."); }
    }
    private sealed class InvalidSceneManager
    {
        public int CurrentScene => 0;
        public void SetScene(int scene, bool instant) { }
    }
    private sealed class ModernText
    {
        // Deliberately use nonstandard ordinals: bind the enum member's name.
        public enum Wrapping { NoWrap = 3, Normal = 17 }
        public Wrapping textWrappingMode { get; set; } = Wrapping.NoWrap;
        public bool enableWordWrapping { set { throw new Exception("Used the obsolete setter."); } }
    }
    private sealed class LegacyText
    {
        public bool enableWordWrapping { get; set; }
    }
    public static int Main()
    {
        Test("Reproduces the v0.1.0 nested Scene lookup failure", () => {
            var manager = typeof(NestedSceneManager);
            Check(manager.GetNestedType("Scene") != null, "Fixture has no nested enum.");
            Check(manager.Assembly.GetType("Scene", false) == null,
                "A standalone Scene type would hide the original failure.");
        });
        Test("Scene binding resolves and invokes the actual nested enum overload", () => {
            var method = SceneTransitionBinding.Resolve(typeof(NestedSceneManager));
            var manager = new NestedSceneManager();
            method.Invoke(manager, new object[] { NestedSceneManager.Scene.Workroom, false });
            Check(manager.CurrentScene == NestedSceneManager.Scene.Workroom,
                "The correct transition overload was not invoked.");
        });
        Test("An incompatible scene API is rejected before patching", () => {
            bool rejected = false;
            try { SceneTransitionBinding.Resolve(typeof(InvalidSceneManager)); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "An integer property was accepted as the game's scene enum.");
        });
        Test("Wrapping explicitly overrides modern TMP's no-wrap default by name", () => {
            var text = new ModernText();
            TextLayoutBinding.EnableWrapping(text);
            Check(text.textWrappingMode == ModernText.Wrapping.Normal, "Text would remain unwrapped.");
        });
        Test("Wrapping explicitly overrides the legacy boolean default", () => {
            var text = new LegacyText();
            TextLayoutBinding.EnableWrapping(text);
            Check(text.enableWordWrapping, "Legacy TMP would remain unwrapped.");
        });
        Test("An unsupported wrapping API produces a useful error", () => {
            bool rejected = false;
            try { TextLayoutBinding.EnableWrapping(new object()); }
            catch (MissingMemberException error) { rejected = error.Message.Contains("textWrappingMode"); }
            Check(rejected, "An unsupported text component silently kept its default.");
        });
        Test("Busy desk cannot open mail", () => {
            var s = new MailSession();
            Check(!s.Open(false, 1) && !s.IsOpen && s.UnreadCount == MailCatalog.Letters.Count, "Blocked open changed state.");
        });
        Test("Repeated open cannot reset a letter being read", () => {
            var s = new MailSession(); s.Open(true, 1); s.SelectLetter(0); s.Turn(1);
            Check(!s.Open(true, 2) && s.IsReading && s.Page == 1, "Duplicate opening reset the page.");
        });
        Test("Page navigation stays within both ends", () => {
            var s = new MailSession(); s.Open(true, 1); s.SelectLetter(0);
            for (int i = 0; i < 20; i++) s.Turn(1);
            Check(s.Page == s.CurrentLetter.Pages.Count - 1, "Advanced past final page.");
            for (int i = 0; i < 20; i++) s.Turn(-1);
            Check(s.Page == 0, "Moved before first page.");
        });
        Test("Putting down a letter marks it read and permits reopening", () => {
            var s = new MailSession(); s.Open(true, 1); s.SelectLetter(0); s.Turn(1); s.Close(true, 2);
            Check(!s.IsOpen && s.IsRead(0) && !s.IsRead(1), "Read flag missing or applied to another letter.");
            Check(s.Open(true, 3) && s.SelectLetter(0) && s.Page == 0 && s.IsRead(0), "Reopen lost state.");
        });
        Test("Scene cancellation does not mark unread mail read", () => {
            var s = new MailSession(); s.Open(true, 1); s.SelectLetter(0); s.Close(false, 2); s.Close(true, 2);
            Check(!s.IsOpen && !s.IsRead(0), "Cancellation or duplicate close marked read.");
        });
        Test("Opening and closing the inbox leaves every letter unread", () => {
            var s = new MailSession(); s.Open(true, 1);
            Check(s.View == MailView.Inbox && s.CurrentLetter == null, "Open bypassed the letter list.");
            s.Turn(1); s.Close(true, 2);
            Check(s.UnreadCount == MailCatalog.Letters.Count, "Browsing the list marked letters read.");
        });
        Test("Returning to the inbox reads only the selected letter", () => {
            var s = new MailSession(); s.Open(true, 1); s.SelectLetter(3); s.Turn(1);
            Check(s.BackToInbox() && s.View == MailView.Inbox && s.Page == 0, "Back failed to restore the list.");
            Check(s.IsRead(3) && !s.IsRead(0) && s.UnreadCount == MailCatalog.Letters.Count - 1,
                "Read state leaked between correspondents.");
            Check(s.SelectLetter(1) && s.CurrentLetter.Id == MailCatalog.Letters[1].Id && s.Page == 0,
                "Selecting another sender retained the old letter or page.");
        });
        Test("Invalid or late selection cannot replace the current letter", () => {
            var s = new MailSession();
            Check(!s.SelectLetter(0), "A closed inbox accepted a stale click.");
            s.Open(true, 1);
            Check(!s.SelectLetter(-1) && !s.SelectLetter(MailCatalog.Letters.Count), "Accepted an invalid index.");
            s.SelectLetter(2); s.Turn(1);
            Check(!s.SelectLetter(4) && s.SelectedIndex == 2 && s.Page == 1, "A duplicate callback changed readers.");
        });
        Test("Unread indication clears only after every letter has been read", () => {
            var s = new MailSession(); s.Open(true, 1);
            for (int i = 0; i < MailCatalog.Letters.Count; i++)
            {
                Check(s.HasUnread, "Unread indicator cleared too soon.");
                s.SelectLetter(i); s.BackToInbox();
                s.SelectLetter(i); s.BackToInbox(); // rereading must not decrement twice
                Check(s.UnreadCount == MailCatalog.Letters.Count - i - 1, "Unread count drifted.");
            }
            Check(!s.HasUnread && s.UnreadCount == 0, "Unread indicator remained after reading every letter.");
        });
        Test("Compiled catalog has distinct identities and all three stationery profiles", () => {
            var ids = new HashSet<string>(); var profiles = new HashSet<StationeryProfile>();
            foreach (var letter in MailCatalog.Letters)
            {
                Check(ids.Add(letter.Id), "Duplicate persistent letter identity: " + letter.Id);
                Check(!string.IsNullOrWhiteSpace(letter.Preview) && !string.IsNullOrWhiteSpace(letter.Archetype), "Missing selection or character data.");
                profiles.Add(letter.Stationery);
            }
            Check(profiles.Count == 3, "One stationery profile has no sample letter.");
        });
        Test("Old holder defaults upgrade once", () => {
            float x = -110, width = 160;
            HolderLayout.Migrate(0, ref x, ref width);
            Check(x == -240 && width == 300, "The existing config would keep the old small holder.");
            x = -110; width = 160;
            HolderLayout.Migrate(HolderLayout.Revision, ref x, ref width);
            Check(x == -110 && width == 160, "A subsequent intentional setting was overwritten.");
        });
        Test("The 0.1.3 holder grows without changing its approved position", () => {
            float x = -240, width = 220;
            HolderLayout.Migrate(1, ref x, ref width);
            Check(x == -240 && width == 300, "The second upgrade changed placement or kept the small size.");
            width = 160;
            HolderLayout.Migrate(1, ref x, ref width);
            Check(width == 160, "A deliberately restored old width was treated as the 0.1.3 default.");
        });
        Test("Holder upgrade preserves custom values independently", () => {
            float x = -330, width = 160;
            HolderLayout.Migrate(0, ref x, ref width);
            Check(x == -330 && width == 300, "Custom position was lost.");
            x = -110; width = 280;
            HolderLayout.Migrate(0, ref x, ref width);
            Check(x == -240 && width == 280, "Custom size was lost.");
            width = 220;
            HolderLayout.Migrate(0, ref x, ref width);
            Check(width == 220, "A custom pre-0.1.3 size was mistaken for a later default.");
        });
        Test("Holder clearance prevents overlap across supported sizes", () => {
            foreach (float width in new[] { 60f, 160f, 220f, 300f, 400f })
                foreach (float request in new[] { -900f, -240f, -110f, 100f })
                {
                    float left = 73f;
                    float resolved = HolderLayout.ClearBell(request, left, width);
                    Check(resolved + width * 0.5f <= left - HolderLayout.BellGap + 0.001f, "Holder overlaps the bell.");
                    Check(resolved <= request, "Clearance unexpectedly moved the holder right.");
                }
        });
        Test("Cancelling an opening cannot later complete or steal reader focus", () => {
            var opening = new OpeningMotion(); opening.Start();
            Check(!opening.Advance(0.3f) && opening.Active, "Animation ended before cancellation.");
            opening.Cancel();
            Check(!opening.Advance(5f) && !opening.Active, "A closed letter retained a completion callback.");
            opening.Start();
            Check(opening.Progress == 0 && !opening.Advance(0), "Reopening retained a previous animation position.");
        });
        Test("A long frame finishes the opening once without overshooting", () => {
            var opening = new OpeningMotion(); opening.Start();
            Check(opening.Advance(10f) && !opening.Active && opening.Progress == 1f, "Long frame left the reader blocked.");
            Check(!opening.Advance(10f), "Completion was delivered twice.");
        });
        Test("Escape is consumed for exactly its closing frame", () => {
            var s = new MailSession(); s.Open(true, 4);
            Check(s.BlocksOptions(5), "Modal did not suppress pause toggle.");
            s.Close(true, 6);
            Check(s.BlocksOptions(6) && !s.BlocksOptions(7), "Escape leaked or suppression persisted.");
        });
        Test("Deferred native presentation resumes exactly once", () => {
            bool open = true; int calls = 0;
            var native = Presentation();
            var wait = PresentationDeferral.Wait(() => open, () => true, () => { calls++; return native; });
            Check(wait.MoveNext() && wait.Current == null && calls == 0, "Presentation ran before close.");
            Check(wait.MoveNext() && calls == 0, "Waiting invoked presentation.");
            open = false;
            Check(wait.MoveNext() && ReferenceEquals(wait.Current, native) && calls == 1, "Native enumerator was lost.");
            Check(!wait.MoveNext() && calls == 1, "Presentation ran twice.");
        });
        Test("Destroying the desk while waiting cancels resumption", () => {
            bool alive = true; int calls = 0;
            var wait = PresentationDeferral.Wait(() => true, () => alive, () => { calls++; return Presentation(); });
            Check(wait.MoveNext(), "Did not begin waiting."); alive = false;
            Check(!wait.MoveNext() && calls == 0, "Resumed on a dead desk.");
        });
        Test("Closing before the deferred coroutine starts still resumes", () => {
            int calls = 0;
            var wait = PresentationDeferral.Wait(() => false, () => true, () => { calls++; return Presentation(); });
            Check(wait.MoveNext() && calls == 1, "Lost the pending native presentation.");
        });
        Test("No available native presentation completes cleanly", () => {
            int calls = 0;
            var wait = PresentationDeferral.Wait(() => false, () => true, () => { calls++; return null; });
            Check(!wait.MoveNext() && calls == 1, "Null native result did not complete.");
        });
        Test("A destroyed desk cannot resume even after mail closes", () => {
            int calls = 0;
            var wait = PresentationDeferral.Wait(() => false, () => false, () => { calls++; return Presentation(); });
            Check(!wait.MoveNext() && calls == 0, "Native code ran after scene teardown.");
        });
        Test("Inbox and reader transitions retain the deferred presentation", () => {
            var s = new MailSession(); s.Open(true, 1); int calls = 0;
            var wait = PresentationDeferral.Wait(() => s.IsOpen, () => true, () => { calls++; return Presentation(); });
            Check(wait.MoveNext() && calls == 0, "Inbox released the presentation early.");
            s.SelectLetter(0);
            Check(wait.MoveNext() && calls == 0, "Selecting mail released the presentation.");
            s.BackToInbox();
            Check(wait.MoveNext() && calls == 0, "Returning to the list released the presentation.");
            s.Close(true, 2);
            Check(wait.MoveNext() && calls == 1 && !wait.MoveNext(), "Closing failed to resume exactly once.");
        });
        Test("Only completed success unlocks Juniper's return letter", () => {
            var q = SampleQuests.Find("juniper.stargazing.02");
            Check(SampleQuests.Status(q, true, false, false, _ => false) == CommissionStatus.Unavailable,
                "Reading, accepting or failing the first request unlocked the follow-up.");
            Check(SampleQuests.Status(q, true, false, false, id => id == "juniper.observatory.01") == CommissionStatus.Available,
                "The successful predecessor did not unlock the follow-up.");
        });
        Test("A restored active or completed quest is never offered a second time", () => {
            foreach (var q in SampleQuests.All)
            {
                Check(SampleQuests.Status(q, true, true, false, _ => false) == CommissionStatus.Active, "Reloaded active quest offered again.");
                Check(SampleQuests.Status(q, true, false, true, _ => true) == CommissionStatus.Completed, "Completed quest offered again.");
                Check(SampleQuests.Status(q, false, false, false, _ => true) == CommissionStatus.Unavailable, "A title-screen action could accept work.");
            }
        });
        Test("Every playable letter contains the exact requirements used for scoring", () => {
            var ids = new HashSet<string>();
            foreach (var q in SampleQuests.All)
            {
                Check(ids.Add(q.RegistryName) && !q.RegistryName.Contains("0.2.0"), "Save identity is duplicated or release-specific.");
                LetterDefinition letter = null;
                foreach (var l in MailCatalog.Letters) if (l.Id == q.LetterId) letter = l;
                Check(letter != null && letter.Pages[letter.Pages.Count - 1] == q.Terms, "Letter and scoring terms diverged.");
                float coverage = 0;
                foreach (var rule in q.Rules)
                {
                    Check(rule.Value > 0 && rule.Value <= 100, "Requirement is out of bounds.");
                    Check(q.Terms.Contains(rule.Description), "A scored requirement is hidden.");
                    if (rule.Type == "colorPercentage") coverage += rule.Value;
                }
                Check(coverage <= 100 && q.Payment > 0, "The dress requirements or payment are impossible.");
                Check(q.After == null || SampleQuests.Find(q.After) != null, "Follow-up refers to an unknown predecessor.");
            }
            Check(ids.Count == 3, "Expected three playable commissions.");
        });
        Test("Two initial requests are independent and four letters remain clearly non-playable", () => {
            int playable = 0, samples = 0, initial = 0;
            foreach (var l in MailCatalog.Letters)
            {
                var q = SampleQuests.Find(l.Id);
                var status = SampleQuests.Status(q, true, false, false, _ => false);
                if (q == null) { samples++; Check(status == CommissionStatus.LetterOnly, "Writing sample acquired an accept action."); }
                else { playable++; if (status == CommissionStatus.Available) initial++; }
            }
            Check(playable == 3 && samples == 4 && initial == 2, "Unexpected initial commission pool.");
        });
        Test("A locked follow-up neither flashes unread nor accepts a stale click", () => {
            var s = new MailSession(); int followup = MailCatalog.Letters.Count - 1;
            s.SetAvailable(followup, false); s.Open(true, 1);
            Check(!s.SelectLetter(followup), "A hidden letter could be opened.");
            for (int i = 0; i < followup; i++) { s.SelectLetter(i); s.BackToInbox(); }
            Check(!s.HasUnread && s.AvailableCount == followup, "A locked letter kept the holder flashing.");
            s.SetAvailable(followup, true);
            Check(s.UnreadCount == 1 && s.SelectLetter(followup), "The unlocked letter was not newly unread and selectable.");
        });
        Test("Delivery replies do not consume or corrupt inbox read counts", () => {
            var s = new MailSession(); int unread = s.UnreadCount;
            s.ShowNotice(new LetterDefinition("receipt", "Juniper", "", "", "Thank you", "", StationeryProfile.Celestial, "Thanks."), 5);
            Check(s.IsNotice && s.IsOpen && s.BlocksOptions(5) && s.CurrentLetter.Id == "receipt", "Reply did not enter the modal reader.");
            s.Turn(100); Check(s.Page == 0, "Single-page reply overflowed.");
            s.Close(true, 6);
            Check(s.UnreadCount == unread && !s.IsNotice && s.CurrentLetter == null, "Closing a reply marked an unrelated letter read.");
        });
        Test("Changing saves clears reading and gating state", () => {
            var s = new MailSession(); s.Open(true, 1); s.SelectLetter(0); s.Close(true, 2);
            s.SetAvailable(6, false); s.ResetReading();
            Check(!s.IsRead(0) && s.IsAvailable(6) && s.UnreadCount == MailCatalog.Letters.Count, "Old save's state leaked into the next save.");
        });
        Test("Repeated delivery callbacks cannot enter completion together", () => {
            var gate = new DeliveryGate(); var state = new object();
            Check(gate.TryBegin(state) && !gate.TryBegin(state), "Concurrent callbacks could pay twice.");
            gate.BeginRewards(state); gate.End(state);
            Check(!gate.TryBegin(state), "A finished delivery could pay again.");
        });
        Test("An adjustment request can be resubmitted without blocking other clients", () => {
            var gate = new DeliveryGate(); var juniper = new object(); var beatrice = new object();
            Check(gate.TryBegin(juniper), "First delivery refused."); gate.End(juniper);
            Check(gate.TryBegin(juniper) && gate.TryBegin(beatrice), "A failed check blocked retry or another commission.");
        });
        Test("A native error after rewards begin cannot silently retry payment", () => {
            var gate = new DeliveryGate(); var state = new object();
            gate.TryBegin(state);
            try { gate.BeginRewards(state); throw new InvalidOperationException("Native completion interrupted."); }
            catch (InvalidOperationException) { }
            finally { gate.End(state); }
            Check(!gate.TryBegin(state), "An exception released the payment guard.");
            gate.Reset();
            Check(gate.TryBegin(new object()), "Loading another save kept obsolete completion state.");
        });
        Console.WriteLine(passed + " regression and behavior checks passed.");
        return 0;
    }
}
