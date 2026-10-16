using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dressmaker.DeskMail
{
    internal enum CommissionStatus { LetterOnly, Unavailable, Available, Active, Completed }

    internal sealed class ScoredRule
    {
        public readonly string Type, Color;
        public readonly float Value;
        public ScoredRule(string type, float value, string color = null)
        { Type = type; Value = value; Color = color; }
        public string Description => Type == "quality" ?
            "Quality score of at least " + Value.ToString("0", CultureInfo.InvariantCulture) :
            "At least " + Value.ToString("0", CultureInfo.InvariantCulture) + "% " + Color.ToLowerInvariant() + " fabric area";
    }

    internal sealed class SampleQuest
    {
        public readonly string LetterId, ClientId, Sender, Title, After, Thanks;
        public readonly int Payment;
        public readonly IReadOnlyList<ScoredRule> Rules;
        public string RegistryName => SampleQuests.Namespace + "." + LetterId;
        public SampleQuest(string id, string client, string sender, string title, int payment,
            string after, string thanks, params ScoredRule[] rules)
        {
            LetterId = id; ClientId = client; Sender = sender; Title = title;
            Payment = payment; After = after; Thanks = thanks;
            Rules = Array.AsReadOnly(rules);
        }
        public string Terms
        {
            get
            {
                var text = new StringBuilder("Our agreement\n\nDelivery requirements\n");
                foreach (var rule in Rules) text.Append("- ").Append(rule.Description).Append(".\n");
                text.Append("\nPayment on delivery: ").Append(Payment).Append(" coins.\n\n")
                    .Append("Silver, ivory, sleeve shapes and other decorative wishes in my letter are suggestions. Only the requirements above are scored. The game's colour categories determine the fabric percentages.\n\n")
                    .Append("Choose 'Take measurements' below, then confirm the measuring sheet to accept. Your other commissions can remain in progress. Deliver through the usual dressmaking controls; I shall reply by letter.\n\n")
                    .Append("If an adjustment is needed, you keep the dress and can try again.");
                return text.ToString();
            }
        }
    }

    internal static class SampleQuests
    {
        // These names are permanent save identities. Never include a release number.
        public const string Namespace = "community.dressmaker.deskmail.samples";
        public static readonly IReadOnlyList<SampleQuest> All = Array.AsReadOnly(new[]
        {
            new SampleQuest("juniper.observatory.01", "juniper", "Juniper Reed", "An evening among the stars", 600, null,
                "Dear dressmaker,\n\nThe unveiling was a triumph. I remembered all three sentences, the telescope behaved, and the moth had the courtesy to leave before the patrons arrived.\n\nYour blue dress made me feel rather like a person who belongs beneath a grand dome. I even climbed the stairs without consulting my feet.\n\nPlease accept my payment, and my warmest thanks. There is talk of a public stargazing evening; if it comes to anything, you shall hear from me again.\n\nYours, with considerably steadier hands,\nJuniper Reed",
                new ScoredRule("quality", 40), new ScoredRule("colorPercentage", 45, "Blue")),
            new SampleQuest("beatrice.conservatory.01", "beatrice", "Beatrice Briar", "A rose with excellent timing", 650, null,
                "Dear dressmaker,\n\nThe rose opened at precisely half past seven. I was almost as well turned out, and considerably more punctual than the chairman.\n\nYour green dress survived the speech, the dancing, and one affectionate encounter with an orange tree. I felt like the hostess of a celebration, rather than its emergency gardener.\n\nYour payment is enclosed. Next time you pass the conservatory, do come in. I have reserved the least argumentative chair for you.\n\nWith grateful affection,\nBeatrice Briar",
                new ScoredRule("quality", 40), new ScoredRule("colorPercentage", 45, "Green")),
            new SampleQuest("juniper.stargazing.02", "juniper", "Juniper Reed", "A little moonlight for everyone", 850, "juniper.observatory.01",
                "Dear dressmaker,\n\nWe filled the roof with visitors. A child asked whether the moon was cold, a gentleman asked whether it was for sale, and nobody fell down the stairs.\n\nThe blue and white of your dress caught the lamplight beautifully. Several people assumed I was in charge. To my considerable surprise, I was.\n\nYour payment is enclosed, with thanks for helping a very nervous assistant grow into her own evening. I hope we shall have many more occasions to correspond.\n\nUnder the same kindly stars,\nJuniper Reed",
                new ScoredRule("quality", 50), new ScoredRule("colorPercentage", 35, "Blue"),
                new ScoredRule("colorPercentage", 15, "White"))
        });

        public static SampleQuest Find(string letterId)
        {
            foreach (var quest in All) if (quest.LetterId == letterId) return quest;
            return null;
        }

        public static CommissionStatus Status(SampleQuest quest, bool hasSave, bool active,
            bool completed, Func<string, bool> succeeded)
        {
            if (quest == null) return CommissionStatus.LetterOnly;
            if (!hasSave) return CommissionStatus.Unavailable;
            if (active) return CommissionStatus.Active;
            if (completed) return CommissionStatus.Completed;
            return quest.After == null || succeeded(quest.After) ? CommissionStatus.Available : CommissionStatus.Unavailable;
        }
    }

    // A failed check may be retried. Once rewards begin, the same saved QuestState
    // must never enter completion twice, even if a later native callback throws.
    internal sealed class DeliveryGate
    {
        private readonly HashSet<object> running = new HashSet<object>();
        private readonly HashSet<object> committed = new HashSet<object>();
        public bool TryBegin(object state) => state != null && !committed.Contains(state) && running.Add(state);
        public void BeginRewards(object state)
        {
            if (!running.Contains(state)) throw new InvalidOperationException("No delivery is in progress.");
            committed.Add(state);
        }
        public void End(object state) => running.Remove(state);
        public void Reset() { running.Clear(); committed.Clear(); }
    }
}
