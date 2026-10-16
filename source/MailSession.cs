using System;
using System.Collections;
using System.Collections.Generic;

namespace Dressmaker.DeskMail
{
    internal enum MailView { Closed, Inbox, Letter }

    // Per-session reading state only. Browsing never creates or accepts a commission.
    internal sealed class MailSession
    {
        private readonly HashSet<string> readIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<int> unavailable = new HashSet<int>();
        private LetterDefinition notice;
        public MailView View { get; private set; }
        public bool IsOpen => View != MailView.Closed;
        public bool IsReading => View == MailView.Letter;
        public int SelectedIndex { get; private set; } = -1;
        public int Page { get; private set; }
        public int UnreadCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MailCatalog.Letters.Count; i++) if (IsAvailable(i) && !IsRead(i)) count++;
                return count;
            }
        }
        public int AvailableCount => MailCatalog.Letters.Count - unavailable.Count;
        public bool HasUnread => UnreadCount > 0;
        public LetterDefinition CurrentLetter => IsReading ? notice ?? MailCatalog.Letters[SelectedIndex] : null;
        public bool IsNotice => IsReading && notice != null;
        public int BlockOptionsThroughFrame { get; private set; } = -1;
        public bool IsRead(int index) => readIds.Contains(MailCatalog.Letters[index].Id);
        public bool IsAvailable(int index) => index >= 0 && index < MailCatalog.Letters.Count && !unavailable.Contains(index);
        public void SetAvailable(int index, bool available)
        {
            if (index < 0 || index >= MailCatalog.Letters.Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (available) unavailable.Remove(index); else unavailable.Add(index);
        }
        public void ResetReading() { readIds.Clear(); unavailable.Clear(); }
        public void ShowNotice(LetterDefinition letter, int frame)
        {
            if (letter == null) throw new ArgumentNullException(nameof(letter));
            notice = letter; SelectedIndex = -1; Page = 0; View = MailView.Letter;
            BlockOptionsThroughFrame = frame;
        }

        public bool Open(bool deskAvailable, int frame)
        {
            if (!deskAvailable || IsOpen) return false;
            View = MailView.Inbox;
            notice = null;
            SelectedIndex = -1;
            Page = 0;
            BlockOptionsThroughFrame = frame;
            return true;
        }

        public bool SelectLetter(int index)
        {
            if (View != MailView.Inbox || !IsAvailable(index)) return false;
            SelectedIndex = index;
            Page = 0;
            View = MailView.Letter;
            return true;
        }

        public bool BackToInbox()
        {
            if (!IsReading) return false;
            MarkCurrentRead();
            notice = null;
            View = MailView.Inbox;
            Page = 0;
            return true;
        }

        public void Close(bool markRead, int frame)
        {
            if (!IsOpen) return;
            if (markRead) MarkCurrentRead();
            notice = null;
            View = MailView.Closed;
            SelectedIndex = -1;
            Page = 0;
            BlockOptionsThroughFrame = frame;
        }

        private void MarkCurrentRead()
        {
            if (IsReading && notice == null) readIds.Add(CurrentLetter.Id);
        }

        public void Turn(int direction)
        {
            if (IsReading) Page = Math.Max(0, Math.Min(CurrentLetter.Pages.Count - 1, Page + direction));
        }

        public bool BlocksOptions(int frame) => IsOpen || frame <= BlockOptionsThroughFrame;
    }

    internal static class PresentationDeferral
    {
        public static IEnumerator Wait(Func<bool> mailOpen, Func<bool> deskAlive,
                                       Func<IEnumerator> nextPresentation)
        {
            while (mailOpen())
            {
                if (!deskAlive()) yield break;
                yield return null;
            }
            if (!deskAlive()) yield break;
            var next = nextPresentation();
            if (next != null) yield return next;
        }
    }
}
