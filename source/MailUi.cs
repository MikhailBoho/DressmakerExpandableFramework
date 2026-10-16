using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Dressmaker.DeskMail
{
    // Fresh UI only. Native graphics/fonts are read, never cloned with game callbacks.
    internal sealed class MailUi : IDisposable
    {
        private readonly Type imageType = Reflect.Type("UnityEngine.UI.Image");
        private readonly Type buttonType = Reflect.Type("UnityEngine.UI.Button");
        private readonly Type tmpType = Reflect.Type("TMPro.TextMeshProUGUI");
        private readonly Type eventSystemType = Reflect.Type("UnityEngine.EventSystems.EventSystem");
        private readonly List<Component> letterTexts = new List<Component>();
        private readonly List<Component> interfaceTexts = new List<Component>();
        private readonly MailSession session;
        private readonly Action open, close, back, commission;
        private readonly Func<string, CommissionStatus> commissionStatus;
        private readonly Action<int> choose, turn;
        private readonly RectTransform overlay, inbox, paper, bodyViewport, bodyContent, listViewport, listContent;
        private readonly Component inboxCount, paperImage, heading, subject, body, bodyScroll, scrollHint;
        private readonly Component counter, previous, next, backButton, listScroll;
        private readonly Component commissionButton, commissionLabel;
        private readonly Component[] rows, readStatus;
        private readonly EmbeddedArtwork holderArtwork, inboxArtwork;
        private readonly EnvelopeVisual envelope;
        private readonly CanvasGroup paperGroup;
        private readonly OpeningMotion opening = new OpeningMotion();
        private EmbeddedArtwork stationeryArtwork;
        private string loadedStationery;
        private RectTransform holder;
        private Component holderImage, holderButton, bell;
        private readonly Vector3[] bellCorners = new Vector3[4];
        private object font, interfaceFont;
        private Sprite nativePaper;
        private GameObject previousSelection;
        private Vector2 lastViewport;
        private int lastScreenWidth, lastScreenHeight;
        private const float RowHeight = 86f, RowGap = 8f;
        public bool HasNativePaper { get; private set; }

        public MailUi(Transform owner, GameBridge bridge, MailSession session, Action open,
            Action close, Action back, Action<int> choose, Action<int> turn,
            Action commission, Func<string, CommissionStatus> commissionStatus)
        {
            this.session = session;
            this.open = open; this.close = close; this.back = back;
            this.choose = choose; this.turn = turn;
            this.commission = commission; this.commissionStatus = commissionStatus;
            font = bridge.Handwriting(tmpType);
            if (!Reflect.Alive(font)) throw new InvalidOperationException("The desk's text font is not ready yet.");
            interfaceFont = font;
            overlay = Rect("DeskMail.Overlay", owner);
            overlay.gameObject.SetActive(false);
            try
            {
                holderArtwork = EmbeddedArtwork.Load("mail-holder.png", true);
                inboxArtwork = EmbeddedArtwork.Load("stationery-rose.png");
                var canvas = overlay.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 30000;
                var scaler = overlay.gameObject.AddComponent(Reflect.Type("UnityEngine.UI.CanvasScaler"));
                Reflect.SetEnum(scaler, "uiScaleMode", "ScaleWithScreenSize");
                Reflect.Set(scaler, "referenceResolution", new Vector2(1920, 1080));
                Reflect.Set(scaler, "matchWidthOrHeight", 0.5f);
                overlay.gameObject.AddComponent(Reflect.Type("UnityEngine.UI.GraphicRaycaster"));
                var shade = Rect("Shade", overlay);
                Stretch(shade, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image(shade, new Color(0.12f, 0.07f, 0.05f, 0.36f), true);

                inbox = Rect("Inbox", overlay);
                inbox.sizeDelta = new Vector2(800, 1000);
                var inboxImage = Image(inbox, Color.white, true);
                Reflect.Set(inboxImage, "sprite", inboxArtwork.Sprite);
                var title = Text("InboxTitle", inbox, "Correspondence", 34, 514);
                Box(title, 0.10f, 0.785f, 0.90f, 0.835f);
                FitCaption(title, 28, 34);
                Reflect.Set(title, "color", new Color(0.36f, 0.17f, 0.17f));
                inboxCount = Text("InboxCount", inbox, "", 18, 514, true);
                Box(inboxCount, 0.10f, 0.742f, 0.90f, 0.785f);
                listViewport = Rect("LetterListViewport", inbox);
                Box(listViewport, 0.10f, 0.17f, 0.90f, 0.735f);
                Image(listViewport, Color.clear, true);
                listViewport.gameObject.AddComponent(Reflect.Type("UnityEngine.UI.RectMask2D"));
                listContent = Rect("LetterList", listViewport);
                TopContent(listContent);
                rows = new Component[MailCatalog.Letters.Count];
                readStatus = new Component[rows.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    int index = i;
                    var letter = MailCatalog.Letters[i];
                    var row = Rect("Letter." + letter.Id, listContent);
                    TopContent(row);
                    row.sizeDelta = new Vector2(0, RowHeight);
                    row.anchoredPosition = new Vector2(0, -i * (RowHeight + RowGap));
                    rows[i] = Clickable(row, () => this.choose(index), true);
                    var name = Text("Sender", row, letter.Sender, 26, 514);
                    Box(name, 0.14f, 0.40f, 0.86f, 0.98f);
                    FitCaption(name, 20, 26);
                    var occasion = Text("Occasion", row, letter.Subject, 19, 514, true);
                    Box(occasion, 0.14f, 0.04f, 0.86f, 0.47f);
                    FitCaption(occasion, 16, 19);
                    readStatus[i] = Text("ReadStatus", row, "Sealed", 14, 514, true);
                    Box(readStatus[i], 0.005f, 0.27f, 0.135f, 0.73f);
                    FitCaption(readStatus[i], 11, 14);
                    var rule = Rect("GoldRule", row);
                    rule.anchorMin = new Vector2(0.10f, 0); rule.anchorMax = new Vector2(0.90f, 0);
                    rule.sizeDelta = new Vector2(0, 0.8f); rule.anchoredPosition = Vector2.zero;
                    Image(rule, new Color(0.59f, 0.43f, 0.24f, 0.55f), false);
                }
                listScroll = Scroll(listViewport, listContent);
                var inboxClose = Button("CloseInbox", inbox, "Put letters away", () => this.close());
                Box(inboxClose, 0.30f, 0.09f, 0.70f, 0.15f);

                envelope = new EnvelopeVisual(overlay);
                paper = Rect("Letter", overlay);
                paper.sizeDelta = new Vector2(800, 1000);
                paperGroup = paper.gameObject.AddComponent<CanvasGroup>();
                paperImage = Image(paper, new Color(0.96f, 0.91f, 0.78f), true);
                heading = Text("Sender", paper, "", 29, 514);
                subject = Text("Subject", paper, "", 23, 514);
                bodyViewport = Rect("ReadingArea", paper);
                Image(bodyViewport, Color.clear, true);
                bodyViewport.gameObject.AddComponent(Reflect.Type("UnityEngine.UI.RectMask2D"));
                body = Text("Body", bodyViewport, "", 28, 257);
                Reflect.SetEnum(body, "overflowMode", "Overflow");
                Reflect.Set(body, "lineSpacing", 1f);
                bodyContent = (RectTransform)body.transform;
                TopContent(bodyContent);
                bodyScroll = Scroll(bodyViewport, bodyContent);
                scrollHint = Text("ScrollHint", paper, "Scroll or drag to read more", 17, 514, true);
                Box(scrollHint, 0.12f, 0.325f, 0.88f, 0.35f);
                commissionButton = Button("Commission", paper, "Read agreement", () => this.commission());
                Box(commissionButton, 0.22f, 0.265f, 0.78f, 0.32f);
                commissionLabel = commissionButton.GetComponentInChildren(tmpType);
                counter = Text("Page", paper, "", 21, 514, true);
                Box(counter, 0.41f, 0.17f, 0.59f, 0.23f);
                previous = Button("Previous", paper, "Previous", () => this.turn(-1));
                Box(previous, 0.12f, 0.17f, 0.38f, 0.23f);
                next = Button("Next", paper, "Next page", () => this.turn(1));
                Box(next, 0.62f, 0.17f, 0.88f, 0.23f);
                backButton = Button("Back", paper, "All letters", () => this.back());
                Box(backButton, 0.19f, 0.09f, 0.48f, 0.15f);
                var readerClose = Button("CloseReader", paper, "Close mail", () => this.close());
                Box(readerClose, 0.52f, 0.09f, 0.81f, 0.15f);
                paper.gameObject.SetActive(false);
                UpdateAssets(bridge);
            }
            catch
            {
                stationeryArtwork?.Dispose(); holderArtwork?.Dispose(); inboxArtwork?.Dispose(); envelope?.Dispose();
                UnityEngine.Object.Destroy(overlay.gameObject);
                throw;
            }
        }

        private RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            return rect;
        }

        private Component Image(RectTransform rect, Color colour, bool raycast)
        {
            var image = rect.gameObject.AddComponent(imageType);
            Reflect.Set(image, "color", colour);
            Reflect.Set(image, "raycastTarget", raycast);
            return image;
        }

        private Component Text(string name, Transform parent, string value, float size, int alignment, bool ui = false)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent(tmpType);
            Reflect.Set(text, "font", ui ? interfaceFont : font);
            Reflect.Set(text, "text", value);
            Reflect.Set(text, "fontSize", size);
            Reflect.Set(text, "color", new Color(0.22f, 0.15f, 0.12f));
            Reflect.Set(text, "raycastTarget", false);
            Reflect.Set(text, "richText", false);
            Reflect.Set(text, "enableAutoSizing", false);
            TextLayoutBinding.EnableWrapping(text);
            Reflect.SetEnum(text, "overflowMode", "Ellipsis");
            Reflect.SetEnum(text, "alignment", alignment);
            (ui ? interfaceTexts : letterTexts).Add(text);
            return text;
        }

        private static void FitCaption(Component text, float minimum, float maximum)
        {
            Reflect.Set(text, "fontSizeMin", minimum);
            Reflect.Set(text, "fontSizeMax", maximum);
            Reflect.Set(text, "enableAutoSizing", true);
        }

        private Component Clickable(RectTransform rect, UnityAction action, bool correspondence = false)
        {
            Component image;
            if (correspondence) image = Image(rect, Color.white, true);
            else
            {
                Image(rect, new Color(0.63f, 0.46f, 0.27f), false);
                var inner = Rect("IvoryInterior", rect);
                Stretch(inner, Vector2.zero, Vector2.one, new Vector2(1.2f, 1.2f), new Vector2(-1.2f, -1.2f));
                image = Image(inner, Color.white, true);
            }
            var button = rect.gameObject.AddComponent(buttonType);
            Reflect.Set(button, "targetGraphic", image);
            var colors = Reflect.Get(button, "colors");
            // A faint wash also avoids fully transparent graphic culling on
            // runtimes where culled Graphics are omitted from UI hit testing.
            Reflect.Set(colors, "normalColor", correspondence ? new Color(0.80f, 0.66f, 0.43f, 0.025f) : new Color(0.96f, 0.90f, 0.79f));
            Reflect.Set(colors, "highlightedColor", correspondence ? new Color(0.76f, 0.56f, 0.32f, 0.15f) : new Color(1f, 0.94f, 0.83f));
            Reflect.Set(colors, "pressedColor", correspondence ? new Color(0.60f, 0.32f, 0.25f, 0.24f) : new Color(0.85f, 0.70f, 0.57f));
            Reflect.Set(colors, "selectedColor", correspondence ? new Color(0.76f, 0.56f, 0.32f, 0.13f) : new Color(0.94f, 0.84f, 0.69f));
            Reflect.Set(colors, "disabledColor", new Color(0.85f, 0.82f, 0.74f, 0.70f));
            Reflect.Set(colors, "fadeDuration", 0.12f);
            Reflect.Set(button, "colors", colors);
            ((UnityEvent)Reflect.Get(button, "onClick")).AddListener(action);
            return button;
        }

        private Component Button(string name, Transform parent, string caption, UnityAction action)
        {
            var rect = Rect(name, parent);
            var button = Clickable(rect, action);
            var label = Text(name + ".Label", rect, caption, 21, 514, true);
            FitCaption(label, 17, 21);
            Stretch((RectTransform)label.transform, Vector2.zero, Vector2.one, new Vector2(8, 3), new Vector2(-8, -3));
            return button;
        }

        private Component Scroll(RectTransform viewport, RectTransform content)
        {
            var scroll = viewport.gameObject.AddComponent(Reflect.Type("UnityEngine.UI.ScrollRect"));
            Reflect.Set(scroll, "viewport", viewport);
            Reflect.Set(scroll, "content", content);
            Reflect.Set(scroll, "horizontal", false);
            Reflect.Set(scroll, "vertical", true);
            Reflect.SetEnum(scroll, "movementType", "Clamped");
            Reflect.Set(scroll, "inertia", false);
            Reflect.Set(scroll, "scrollSensitivity", 32f);
            return scroll;
        }

        private static void TopContent(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = low; rect.offsetMax = high;
        }

        private static void Box(Component component, float left, float bottom, float right, float top) =>
            Stretch((RectTransform)component.transform, new Vector2(left, bottom), new Vector2(right, top), Vector2.zero, Vector2.zero);

        public void UpdateAssets(GameBridge bridge)
        {
            var handwriting = bridge.Handwriting(tmpType);
            if (Reflect.Alive(handwriting)) font = handwriting;
            var nativeFont = NativeTypography.MenuFont(bridge.Pigeon);
            interfaceFont = Reflect.Alive(nativeFont) ? nativeFont : font;
            foreach (var text in letterTexts) Reflect.Set(text, "font", font);
            foreach (var text in interfaceTexts) Reflect.Set(text, "font", interfaceFont);
            nativePaper = bridge.PaperSprite;
            HasNativePaper = nativePaper;
            lastViewport = Vector2.zero;
        }

        public void EnsureHolder(GameBridge bridge)
        {
            var currentBell = bridge.Bell;
            if (!currentBell || !(currentBell.transform is RectTransform)) return;
            if (holder && bell == currentBell) return;
            if (holder) UnityEngine.Object.Destroy(holder.gameObject);
            bell = currentBell;
            holder = Rect("DeskMail.Holder", currentBell.transform.parent);
            holder.gameObject.SetActive(false);
            var layout = holder.gameObject.AddComponent(Reflect.Type("UnityEngine.UI.LayoutElement"));
            Reflect.Set(layout, "ignoreLayout", true);
            holderImage = Image(holder, Color.white, true);
            Reflect.Set(holderImage, "sprite", holderArtwork.Sprite);
            Reflect.Set(holderImage, "preserveAspect", true);
            Reflect.Set(holderImage, "alphaHitTestMinimumThreshold", 0.1f);
            holderButton = holder.gameObject.AddComponent(buttonType);
            Reflect.Set(holderButton, "targetGraphic", holderImage);
            Reflect.SetEnum(holderButton, "transition", "None");
            ((UnityEvent)Reflect.Get(holderButton, "onClick")).AddListener(() => open());
        }

        public void UpdateHolder(bool visible, bool interactable, bool hasUnread, float offsetX, float offsetY, float width, bool pulseUnread)
        {
            if (!holder || !bell) return;
            holder.gameObject.SetActive(visible);
            if (!visible) return;
            Reflect.Set(holderButton, "interactable", interactable);
            Reflect.Set(holderImage, "raycastTarget", interactable);
            holder.position = bell.transform.position;
            var position = holder.localPosition + new Vector3(offsetX, offsetY, 0);
            ((RectTransform)bell.transform).GetWorldCorners(bellCorners);
            float bellLeft = float.PositiveInfinity;
            foreach (var corner in bellCorners)
                bellLeft = Mathf.Min(bellLeft, holder.parent.InverseTransformPoint(corner).x);
            position.x = HolderLayout.ClearBell(position.x, bellLeft, width);
            holder.localPosition = position;
            holder.localRotation = Quaternion.identity;
            holder.sizeDelta = new Vector2(width, width / holderArtwork.Aspect);
            var pulse = hasUnread && pulseUnread ? (1f - Mathf.Cos(Time.unscaledTime * Mathf.PI)) * 0.5f : 0f;
            Reflect.Set(holderImage, "color", Color.Lerp(Color.white, new Color(1f, 0.28f, 0.20f), pulse));
        }

        private object EventSystem => eventSystemType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null, null);
        private void Select(GameObject obj)
        {
            var system = EventSystem;
            if (Reflect.Alive(system)) Reflect.Method(eventSystemType, "SetSelectedGameObject", typeof(GameObject)).Invoke(system, new object[] { obj });
        }

        public void ShowInbox(bool opening)
        {
            ResetOpening();
            if (opening)
            {
                var system = EventSystem;
                previousSelection = Reflect.Alive(system) ? Reflect.Get(system, "currentSelectedGameObject") as GameObject : null;
            }
            overlay.gameObject.SetActive(true);
            paper.gameObject.SetActive(false);
            inbox.gameObject.SetActive(true);
            Reflect.Set(inboxCount, "text", session.UnreadCount == 0 ? "Every letter has been opened" :
                session.UnreadCount == 1 ? "One sealed letter awaits you" : session.UnreadCount + " sealed letters await you");
            for (int i = 0; i < readStatus.Length; i++)
            {
                rows[i].gameObject.SetActive(session.IsAvailable(i));
                bool read = session.IsRead(i);
                var status = commissionStatus(MailCatalog.Letters[i].Id);
                Reflect.Set(readStatus[i], "text", status == CommissionStatus.Active ? "In work" :
                    status == CommissionStatus.Completed ? "Delivered" : status == CommissionStatus.LetterOnly ? "Sample" : read ? "Read" : "Sealed");
                Reflect.Set(readStatus[i], "color", read ? new Color(0.42f, 0.38f, 0.33f) : new Color(0.66f, 0.13f, 0.18f));
            }
            lastViewport = Vector2.zero;
            Layout();
            if (opening) Reflect.Set(listScroll, "verticalNormalizedPosition", 1f);
            int index = session.SelectedIndex;
            if (!session.IsAvailable(index))
                for (int i = 0; i < rows.Length; i++) if (session.IsAvailable(i)) { index = i; break; }
            Select(session.IsAvailable(index) ? rows[index].gameObject : null);
        }

        public void ShowLetter(PaperSelection selection, bool animate)
        {
            ResetOpening();
            overlay.gameObject.SetActive(true);
            inbox.gameObject.SetActive(false);
            paper.gameObject.SetActive(true);
            var letter = session.CurrentLetter;
            Reflect.Set(heading, "text", "From " + letter.Sender);
            Reflect.Set(subject, "text", letter.Subject);
            ApplyPaper(letter.Stationery, selection);
            Layout();
            SetPage();
            if (animate)
            {
                opening.Start();
                envelope.Show();
                paperGroup.interactable = paperGroup.blocksRaycasts = false;
                Select(null);
                Animate(0);
            }
            else FocusReader();
        }

        private void FocusReader() => Select((session.CurrentLetter.Pages.Count > 1 ? next : backButton).gameObject);

        public void Animate(float deltaTime)
        {
            if (!opening.Active) return;
            if (opening.Advance(deltaTime)) { ResetOpening(); FocusReader(); return; }
            float t = opening.Progress;
            float reveal = OpeningMotion.Ease(t, 0.38f, 1f);
            float fit = Mathf.Min(1f, Mathf.Min(overlay.rect.width / 760f, overlay.rect.height / 680f));
            envelope.Pose(OpeningMotion.Ease(t, 0.10f, 0.54f),
                1f - OpeningMotion.Ease(t, 0.63f, 0.90f),
                1f - OpeningMotion.Ease(t, 0.08f, 0.26f), fit,
                Mathf.Lerp(-80f, -25f, OpeningMotion.Ease(t, 0, 0.28f)) - 80f * reveal);
            paperGroup.alpha = OpeningMotion.Ease(t, 0.38f, 0.64f);
            paper.localScale = new Vector3(Mathf.Lerp(0.72f, 1f, reveal), Mathf.Lerp(0.08f, 1f, reveal), 1f);
            paper.anchoredPosition = new Vector2(0, Mathf.Lerp(-60f, 0, reveal));
            paper.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-2f, 0, reveal));
        }

        private void ResetOpening()
        {
            opening.Cancel();
            envelope.Root.gameObject.SetActive(false);
            paperGroup.alpha = 1;
            paperGroup.interactable = paperGroup.blocksRaycasts = true;
            paper.localScale = Vector3.one;
            paper.localRotation = Quaternion.identity;
            paper.anchoredPosition = Vector2.zero;
        }

        public void FinishOpening()
        {
            if (!opening.Active) return;
            ResetOpening();
            if (session.IsReading) FocusReader();
        }

        private void ApplyPaper(StationeryProfile profile, PaperSelection selection)
        {
            Sprite sprite;
            bool ornate = selection != PaperSelection.Classic;
            if (ornate)
            {
                switch (selection)
                {
                    case PaperSelection.Celestial: profile = StationeryProfile.Celestial; break;
                    case PaperSelection.Botanical: profile = StationeryProfile.Botanical; break;
                    case PaperSelection.Rose: profile = StationeryProfile.Rose; break;
                }
                var filename = "stationery-" + profile.ToString().ToLowerInvariant() + ".png";
                if (filename != loadedStationery)
                {
                    var replacement = EmbeddedArtwork.Load(filename);
                    stationeryArtwork?.Dispose();
                    stationeryArtwork = replacement;
                    loadedStationery = filename;
                }
                sprite = stationeryArtwork.Sprite;
            }
            else sprite = nativePaper;
            Reflect.Set(paperImage, "sprite", sprite);
            Reflect.Set(paperImage, "color", sprite ? Color.white : new Color(0.96f, 0.91f, 0.78f));
            // Generated letterheads extend through the top 18%; keep all text beneath them.
            Box(heading, 0.11f, ornate ? 0.755f : 0.89f, 0.89f, ornate ? 0.815f : 0.95f);
            Box(subject, 0.11f, ornate ? 0.70f : 0.83f, 0.89f, ornate ? 0.755f : 0.89f);
            Box(bodyViewport, 0.11f, 0.355f, 0.89f, ornate ? 0.68f : 0.81f);
            lastViewport = Vector2.zero;
        }

        public void SetPage()
        {
            var letter = session.CurrentLetter;
            Reflect.Set(body, "text", letter.Pages[session.Page]);
            Reflect.Set(counter, "text", (session.Page + 1) + " / " + letter.Pages.Count);
            Reflect.Set(previous, "interactable", session.Page > 0);
            Reflect.Set(next, "interactable", session.Page + 1 < letter.Pages.Count);
            var status = commissionStatus(letter.Id);
            commissionButton.gameObject.SetActive(!session.IsNotice);
            Reflect.Set(commissionLabel, "text", status == CommissionStatus.Available ?
                (session.Page + 1 < letter.Pages.Count ? "Read agreement" : "Take measurements") :
                status == CommissionStatus.Active ? "Continue commission" :
                status == CommissionStatus.Completed ? "Delivered with thanks" :
                status == CommissionStatus.LetterOnly ? "Correspondence sample" : "Not yet available");
            Reflect.Set(commissionButton, "interactable", status == CommissionStatus.Available || status == CommissionStatus.Active);
            Reflect.Set(backButton.GetComponentInChildren(tmpType), "text", session.IsNotice ? "Put letter away" : "All letters");
            FitBody(true);
        }

        private void FitBody(bool resetScroll)
        {
            var position = resetScroll ? 1f : (float)Reflect.Get(bodyScroll, "verticalNormalizedPosition");
            Canvas.ForceUpdateCanvases();
            var height = (float)Reflect.Get(body, "preferredHeight") + 12f;
            var viewportHeight = bodyViewport.rect.height;
            bodyContent.sizeDelta = new Vector2(0, Mathf.Max(viewportHeight, height));
            Reflect.Set(bodyScroll, "velocity", Vector2.zero);
            Reflect.Set(bodyScroll, "verticalNormalizedPosition", Mathf.Clamp01(position));
            scrollHint.gameObject.SetActive(height > viewportHeight + 1f);
        }

        public void Layout()
        {
            if (!overlay || !overlay.gameObject.activeSelf) return;
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || lastViewport == Vector2.zero)
            {
                lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
                Canvas.ForceUpdateCanvases();
            }
            var size = overlay.rect.size;
            if (size == lastViewport) return;
            lastViewport = size;
            if (inbox.gameObject.activeSelf)
            {
                float width = Mathf.Min(800f, size.x - 40f, (size.y - 40f) * inboxArtwork.Aspect);
                inbox.sizeDelta = new Vector2(width, width / inboxArtwork.Aspect);
                Canvas.ForceUpdateCanvases();
                int visible = 0;
                for (int i = 0; i < rows.Length; i++)
                    if (session.IsAvailable(i)) ((RectTransform)rows[i].transform).anchoredPosition = new Vector2(0, -visible++ * (RowHeight + RowGap));
                listContent.sizeDelta = new Vector2(0, Mathf.Max(listViewport.rect.height, visible * (RowHeight + RowGap) - RowGap));
            }
            if (paper.gameObject.activeSelf)
            {
                float width = Mathf.Min(800f, size.x - 40f), height = Mathf.Min(1000f, size.y - 40f);
                var sprite = Reflect.Get(paperImage, "sprite") as Sprite;
                var aspect = sprite ? sprite.rect.width / sprite.rect.height : 0.8f;
                if (aspect < 0.55f || aspect > 1.15f) aspect = 0.8f;
                width = Mathf.Min(width, height * aspect);
                paper.sizeDelta = new Vector2(width, width / aspect);
                FitBody(false);
            }
        }

        public void Hide()
        {
            ResetOpening();
            if (overlay) overlay.gameObject.SetActive(false);
            Select(previousSelection && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
        }

        public void Dispose()
        {
            if (overlay && overlay.gameObject.activeSelf) Hide();
            if (holder) UnityEngine.Object.Destroy(holder.gameObject);
            if (overlay) UnityEngine.Object.Destroy(overlay.gameObject);
            holderArtwork?.Dispose(); stationeryArtwork?.Dispose(); inboxArtwork?.Dispose(); envelope?.Dispose();
        }
    }
}
