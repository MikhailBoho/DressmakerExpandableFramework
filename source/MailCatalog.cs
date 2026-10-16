using System;
using System.Collections.Generic;

namespace Dressmaker.DeskMail
{
    internal enum StationeryProfile { Celestial, Botanical, Rose }
    internal enum PaperSelection { BySender, Celestial, Botanical, Rose, Classic }

    internal sealed class LetterDefinition
    {
        public readonly string Id, Sender, Role, Archetype, Subject, Preview;
        public readonly StationeryProfile Stationery;
        public readonly IReadOnlyList<string> Pages;

        public LetterDefinition(string id, string sender, string role, string archetype,
            string subject, string preview, StationeryProfile stationery, params string[] pages)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(sender) ||
                string.IsNullOrWhiteSpace(subject) || pages == null || pages.Length == 0)
                throw new ArgumentException("A letter needs an ID, sender, subject, and text.");
            foreach (var page in pages)
                if (string.IsNullOrWhiteSpace(page)) throw new ArgumentException("Letter pages cannot be empty.");
            Id = id; Sender = sender; Role = role; Archetype = archetype;
            Subject = subject; Preview = preview; Stationery = stationery;
            var content = new List<string>(pages);
            var quest = SampleQuests.Find(id);
            if (quest != null) content.Add(quest.Terms);
            Pages = content.AsReadOnly();
        }
    }

    // The first two letters and Juniper's follow-up carry playable commissions.
    // Other correspondents remain writing samples, identified in the reader.
    internal static class MailCatalog
    {
        public static readonly IReadOnlyList<LetterDefinition> Letters = Array.AsReadOnly(new[]
        {
            new LetterDefinition("juniper.observatory.01",
                "Juniper Reed",
                "Observatory assistant",
                "Anxious scholar",
                "An evening among the stars",
                "Deep blue / Silver / Rooftop sleeves",
                StationeryProfile.Celestial,
                SampleLetter.Pages),

            new LetterDefinition("beatrice.conservatory.01",
                "Beatrice Briar",
                "Conservatory gardener",
                "Practical romantic",
                "A rose with excellent timing",
                "Sage green / Ivory / Botanical detail",
                StationeryProfile.Botanical,
                "Dear dressmaker,\n\nAfter seven years of patient persuasion, our climbing rose intends to flower on the very evening the conservatory's patrons visit. I have arranged a small celebration. The rose has arranged to be the centre of attention.\n\nI usually receive guests in an apron containing three trowels and yesterday's biscuit. For once, I should like to look as carefully tended as the plants. Could you make me something in sage green, with ivory touches and a little botanical decoration?\n\nThere will be a short speech, followed by refreshments and, if the committee can be distracted, dancing among the orange trees.",
                "For an evening in the conservatory\n\n- Sage green, softened with ivory.\n- A leaf or floral detail.\n- Sleeves that leave my hands free.\n- A hem clear of damp paving stones.\n\nPlease keep the decoration away from the cuffs. A tendril once mistook my lace for a trellis, and neither of us emerged with our dignity intact.\n\nWith warm thanks,\nBeatrice Briar\nKeeper of the West Conservatory"),

            new LetterDefinition("odette.encore.01",
                "Odette Bellamy",
                "Travelling soprano",
                "Theatrical perfectionist",
                "Something worthy of an encore",
                "Wine red / Gold / Dramatic silhouette",
                StationeryProfile.Rose,
                "My dear dressmaker,\n\nI have been asked to sing at the candlelight recital. The programme calls it an intimate evening. There are two hundred chairs and a chandelier large enough to have opinions.\n\nI am imagining wine red, a little gold, and a silhouette the back row can appreciate. The accompanist assures me that the music alone will command attention. He is wearing a waistcoat embroidered with peacocks, so I consider his advice negotiable.\n\nFor the final song I must raise both arms without raising the entire dress. A small practical matter, though the audience would certainly remember it.",
                "Notes before the overture\n\n- Wine red as the principal colour.\n- Gold details that catch candlelight.\n- A graceful, dramatic skirt.\n- Enough freedom at the shoulders to perform.\n\nNo rattling ornaments, please. I am prepared to compete with the chandelier, but not with my own sleeves.\n\nYours until the final applause,\nOdette Bellamy"),

            new LetterDefinition("emilia.midnight-clock.01",
                "Emilia Vale",
                "Clockmaker",
                "Exacting idealist",
                "The hour we have been waiting for",
                "Ink blue / Brass tones / Neat cuffs",
                StationeryProfile.Celestial,
                "Dear dressmaker,\n\nTomorrow the restored town clock will strike midnight for the first time in forty years. I am to wind it in front of the donors. They have requested formal dress; the clock has requested that nobody touch its escapement.\n\nInk blue with a little brass-coloured decoration would suit the occasion beautifully. I should like tidy cuffs and an uncluttered shape. There are narrow steps, exposed mechanisms, and an alarming number of people who will ask whether I am certain it will work.\n\nI am certain. I have only been checking it every eleven minutes as a courtesy.",
                "A matter of precision\n\n- Ink blue with restrained brass or gold accents.\n- Close, tidy cuffs.\n- A practical hem for the clock-tower stairs.\n- No dangling ribbons near the machinery.\n\nOne discreet ornamental detail would be lovely. I spend my days making beautiful things that nobody sees once the case is shut. This time I should like to leave the case open.\n\nWith appreciation,\nEmilia Vale\nClockmaker"),

            new LetterDefinition("celia.honey-fair.01",
                "Celia Moss",
                "Beekeeper",
                "Cheerful pragmatist",
                "The bees are not invited",
                "Honey yellow / Cream / Simple trim",
                StationeryProfile.Botanical,
                "Hello, dressmaker!\n\nMy honey has won a ribbon at the harvest fair. I have been invited to the supper afterwards, which means I require a dress that has never been used to carry a hive frame.\n\nHoney yellow and cream would make me very happy. Something cheerful, easy to move in, and not too precious for a long table full of neighbours. There will be dancing. There will also be preserves, so the dancing may depend on how much we eat.\n\nThe bees have contributed neither to this letter nor to the guest list. I mention this because the committee asked twice.",
                "For a very ordinary celebration\n\n- Warm honey yellow and cream.\n- Simple, tidy decoration.\n- Comfortable sleeves.\n- A skirt suitable for dancing.\n\nA small flower detail would be welcome. An enormous flower might be misunderstood by someone from work.\n\nThank you kindly,\nCelia Moss\nMoss Orchard Apiary"),

            new LetterDefinition("rosalind.salon.01",
                "Rosalind Aster",
                "Retired dance tutor",
                "Mischievous sophisticate",
                "A waltz after all these years",
                "Dusty rose / Pearl tones / Soft drape",
                StationeryProfile.Rose,
                "Dear dressmaker,\n\nFor thirty years I taught other people how to enter a ballroom. This spring I have accepted an invitation of my own. My former pupils are being unbearably encouraging.\n\nI would like dusty rose, something softly draped, and a little pearl-coloured detail. Nothing severe. I have spent quite enough evenings telling people to straighten their shoulders.\n\nAn old friend has promised me the first waltz. He still counts aloud when he is nervous. Please make something lovely enough to distract everyone from the arithmetic.",
                "One last lesson\n\n- Dusty rose with pearl or ivory accents.\n- Soft, flowing lines.\n- A modest neckline.\n- A hem that permits a proper turning step.\n\nA little elegance at the sleeves would be delightful. The hands must still be visible; I intend to lead if he forgets where we are going.\n\nYours with renewed anticipation,\nRosalind Aster"),

            new LetterDefinition("juniper.stargazing.02",
                "Juniper Reed", "Observatory assistant", "A scholar finding her confidence",
                "A little moonlight for everyone", "Blue / White / Public stargazing",
                StationeryProfile.Celestial,
                "My dear dressmaker,\n\nDo you remember the public evening I mentioned? The committee has approved it. They have also appointed me its hostess, on the alarming grounds that the telescope unveiling went rather well.\n\nFamilies will come up to the roof to see the moon. I want a dress in blue and white: a little piece of the night sky, with enough light in it to be welcoming.\n\nYour first dress helped me feel that I belonged at the observatory. This time I should like everyone else to feel they belong there too.",
                "Preparations for our visitors\n\nBlue should lead the design, with a visible sweep of white. A moonlike detail would delight me, but please leave the choice of decoration to your own good sense.\n\nThere will be children, lamps, and a plate of biscuits that must not be mistaken for part of the demonstration. I shall need to move comfortably between them all.\n\nI have put our precise agreement on the following page. Thank you for accepting another small adventure with me.\n\nWith renewed confidence,\nJuniper Reed")
        });
    }

    internal static class SampleLetter
    {
        public const string Sender = "Juniper Reed";
        public const string Subject = "An evening among the stars";
        // Explicit pages make this small prototype independent of native overflow/reveal logic.
        public static readonly string[] Pages =
        {
            "Dear dressmaker,\n\n" +
            "The observatory has invited me to unveil its new telescope. This is a tremendous honour for someone whose last discovery was a moth asleep inside the lens.\n\n" +
            "I would love a dress fit for an evening among the stars: deep blue, with a little silver that catches the light. Elegant, certainly, but forgiving enough for a nervous astronomer to climb a narrow staircase.\n\n" +
            "Everyone expects a grand speech. I have prepared three sentences and an excellent diagram of the moon. Surely one of those will do.\n\n" +
            "I have put my practical wishes on the other side.",
            "A few practical wishes\n\n" +
            "- Deep blue as the main colour.\n" +
            "- A modest touch of silver.\n" +
            "- Sleeves suitable for a chilly rooftop.\n" +
            "- A hem that will not trip me on the stairs.\n\n" +
            "Please avoid anything that jingles. Our head astronomer insists that silence improves the view, although his own spectacles make a remarkable amount of noise.\n\n" +
            "If the result makes me look as though I know where all the constellations are, I shall consider it a triumph.\n\n" +
            "With hopeful regards,\n" +
            "Juniper Reed\n" +
            "Assistant Keeper of the Observatory"
        };
    }
}
