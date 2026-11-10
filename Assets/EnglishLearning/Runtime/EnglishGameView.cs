using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EnglishLearning
{
    /// <summary>Runtime-generated, safe-area aware UI. No lesson progression rules live here.</summary>
    public sealed class EnglishGameView : MonoBehaviour
    {
        private static readonly Color Navy = new Color32(30, 51, 65, 255);
        private static readonly Color Muted = new Color32(100, 116, 120, 255);
        private static readonly Color Cream = new Color32(247, 246, 237, 255);
        private static readonly Color Gold = new Color32(245, 195, 78, 255);
        private RectTransform safe, content, card, field, modal;
        private TextMeshProUGUI lessonLabel, wordLabel, countLabel, feedback, instruction;
        private Button speaker, homeButton;
        private bool homeNavigationEnabled;
        private TMP_FontAsset font;
        private readonly List<ObjectView> objects = new List<ObjectView>();
        private readonly DemoObjectArt art = new DemoObjectArt();
        private Vector2 previousSize;
        private Rect previousSafeArea;
        public FlashlightOverlay Flashlight { get; private set; }
        public event Action SpeakerPressed, LessonsPressed, RestartPressed;
        public event Action<int> LessonChosen;
        public event Action NextPressed, ReplayPressed, HomePressed;

        private sealed class ObjectView
        {
            public LessonObjectPlacement Placement;
            public RectTransform Rect;
            public Image Image;
            public bool Found;
        }

        public void Build()
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            var canvasObject = new GameObject("Word Explorer UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = .5f;
            var backdrop = Panel(canvasObject.transform, "Background", Cream);
            Stretch(backdrop.rectTransform);
            safe = Rect(canvasObject.transform, "Device safe area"); Stretch(safe);
            content = Rect(safe, "Content");
            content.anchorMin = new Vector2(.5f, 0); content.anchorMax = new Vector2(.5f, 1);
            content.pivot = new Vector2(.5f, .5f);

            lessonLabel = Label(content, "Lesson title", "WORD EXPLORER", 23, Navy);
            Place(lessonLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -12), new Vector2(480, 34));
            lessonLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var menu = Button(content, "Lessons", Cream, () => LessonsPressed?.Invoke());
            Place(menu.GetComponent<RectTransform>(), new Vector2(1,1), new Vector2(1,1), new Vector2(0,-8), new Vector2(144,48));
            card = Panel(content, "Word card", Color.white).rectTransform;
            Place(card, new Vector2(0,1), new Vector2(0,1), new Vector2(0,-66), new Vector2(680,112));
            var eyebrow = Label(card, "Prompt", "FIND THIS OBJECT", 16, Muted);
            Place(eyebrow.rectTransform, new Vector2(0,1), new Vector2(0,1), new Vector2(24,-14), new Vector2(300,24));
            eyebrow.alignment = TextAlignmentOptions.MidlineLeft;
            wordLabel = Label(card, "English word", "", 44, Navy);
            wordLabel.fontStyle = FontStyles.Bold;
            Place(wordLabel.rectTransform, new Vector2(0,0), new Vector2(0,0), new Vector2(24,12), new Vector2(470,58));
            wordLabel.alignment = TextAlignmentOptions.MidlineLeft;
            wordLabel.enableAutoSizing = true; wordLabel.fontSizeMin = 20; wordLabel.fontSizeMax = 44;
            speaker = Button(card, "", Gold, () => SpeakerPressed?.Invoke());
            speaker.gameObject.name = "Pronounce word";
            Place(speaker.GetComponent<RectTransform>(), new Vector2(1,.5f), new Vector2(1,.5f), new Vector2(-16,0), new Vector2(72,72));
            var icon = new GameObject("Speaker icon", typeof(RectTransform), typeof(SpeakerGraphic));
            icon.transform.SetParent(speaker.transform, false);
            var iconRect = icon.GetComponent<RectTransform>();
            Place(iconRect, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(38,38));
            icon.GetComponent<SpeakerGraphic>().color = Navy;
            field = Panel(content, "Object field", new Color(.85f,.91f,.86f)).rectTransform;
            Stretch(field);
            var dark = new GameObject("Flashlight darkness", typeof(RectTransform), typeof(FlashlightOverlay));
            dark.transform.SetParent(field, false);
            Flashlight = dark.GetComponent<FlashlightOverlay>(); Stretch(Flashlight.rectTransform);
            countLabel = Label(content, "Word count", "", 19, Navy);
            Place(countLabel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0,42), new Vector2(140,30));
            countLabel.alignment = TextAlignmentOptions.MidlineLeft;
            homeButton = Button(content, "Home", Cream, () => HomePressed?.Invoke());
            Place(homeButton.GetComponent<RectTransform>(), new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,34), new Vector2(120,40));
            homeButton.gameObject.SetActive(false);
            var restart = Button(content, "Restart", Cream, () => RestartPressed?.Invoke());
            Place(restart.GetComponent<RectTransform>(), new Vector2(1,0), new Vector2(1,0), new Vector2(0,38), new Vector2(120,40));
            feedback = Label(content, "Feedback", "", 18, Muted);
            Place(feedback.rectTransform, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,58), new Vector2(440,28));
            instruction = Label(content, "How to play", "Move to explore. Tap a lit object to choose.", 17, Muted);
            Place(instruction.rectTransform, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,15), new Vector2(650,28));
            modal = Panel(safe, "Dialog layer", new Color(Navy.r,Navy.g,Navy.b,.94f)).rectTransform;
            Stretch(modal); modal.gameObject.SetActive(false);
            Layout(true);
        }

        public void SetHomeNavigation(bool enabled)
        {
            homeNavigationEnabled = enabled;
            homeButton.gameObject.SetActive(enabled);
            feedback.rectTransform.anchoredPosition = new Vector2(0,enabled ? 86 : 58);
            Layout(true);
        }

        private void AddDialogHomeButton(RectTransform panel, float bottom)
        {
            if (!homeNavigationEnabled) return;
            var button = Button(panel, "Home", Cream, () => HomePressed?.Invoke());
            Place(button.GetComponent<RectTransform>(), new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,bottom), new Vector2(260,44));
        }

        public void ShowLesson(EnglishLesson lesson, float radius, float feather)
        {
            CloseDialog();
            foreach (var view in objects) { view.Rect.gameObject.SetActive(false); Destroy(view.Rect.gameObject); }
            objects.Clear();
            field.GetComponent<Image>().color = lesson.background;
            lessonLabel.text = lesson.title;
            foreach (var placement in lesson.objects)
            {
                var rect = Rect(field, placement.wordId);
                rect.anchorMin = rect.anchorMax = placement.position;
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.Euler(0,0,placement.rotation);
                var image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false; image.preserveAspect = true;
                objects.Add(new ObjectView { Placement = placement, Rect = rect, Image = image });
            }
            Flashlight.transform.SetAsLastSibling();
            Flashlight.gameObject.SetActive(true);
            Flashlight.InputEnabled = true;
            Flashlight.Configure(radius, feather);
            SetFeedback(""); Layout(true);
        }

        public void SetObjectSprites(EnglishCourse course)
        {
            foreach (var view in objects)
            {
                var word = course.FindWord(view.Placement.wordId);
                view.Image.sprite = word.sprite != null ? word.sprite : art.Get(word.demoIcon);
            }
        }

        public string HitObject(Vector2 fieldPoint)
        {
            Vector3 worldPoint = field.TransformPoint(fieldPoint);
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var view = objects[i];
                if (!view.Found && view.Rect.rect.Contains(view.Rect.InverseTransformPoint(worldPoint))) return view.Placement.wordId;
            }
            return null;
        }

        public void MarkFound(string id)
        {
            foreach (var view in objects)
                if (view.Placement.wordId == id) { view.Found = true; view.Image.color = new Color(1,1,1,.16f); }
        }

        public void SetWord(string text, int completed, int total)
        {
            wordLabel.text = text;
            countLabel.text = completed + " / " + total + " found";
            speaker.interactable = !string.IsNullOrEmpty(text);
        }

        public void SetFeedback(string text, bool success = false)
        {
            feedback.text = text;
            feedback.color = success ? new Color(.12f,.43f,.32f) : Muted;
        }

        public void ShowLessons(EnglishCourse course, int selected)
        {
            OpenDialog();
            var panel = DialogCard("CHOOSE A LESSON", new Vector2(580,660));
            var scrollRect = Rect(panel, "Lesson scroll"); Stretch(scrollRect);
            scrollRect.offsetMin = new Vector2(24,homeNavigationEnabled ? 140 : 86); scrollRect.offsetMax = new Vector2(-24,-86);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = Panel(scrollRect, "Viewport", Color.white).rectTransform; Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var list = Rect(viewport, "Lessons");
            list.anchorMin = new Vector2(0,1); list.anchorMax = Vector2.one; list.pivot = new Vector2(.5f,1);
            list.sizeDelta = new Vector2(0,course.lessons.Count * 82);
            scroll.viewport = viewport; scroll.content = list; scroll.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < course.lessons.Count; i++)
            {
                int index = i;
                var button = Button(list, (i + 1) + ". " + course.lessons[i].title, i == selected ? Gold : Cream, () => LessonChosen?.Invoke(index));
                var r = button.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0,1); r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f,1);
                r.anchoredPosition = new Vector2(0,-i*82); r.sizeDelta = new Vector2(0,68);
            }
            var close = Button(panel, "Back to game", Navy, CloseDialog, Color.white);
            Place(close.GetComponent<RectTransform>(), new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,homeNavigationEnabled ? 76 : 20), new Vector2(300,50));
            AddDialogHomeButton(panel,20);
        }

        public void ShowComplete(string lessonTitle, bool hasNext)
        {
            Flashlight.gameObject.SetActive(false);
            foreach (var view in objects) view.Image.color = Color.white;
            OpenDialog();
            var panel = DialogCard("LESSON COMPLETE!", new Vector2(580,homeNavigationEnabled ? 470 : 400));
            float homeSpace = homeNavigationEnabled ? 58 : 0;
            var message = Label(panel, "Result", lessonTitle + "\nYou found every word.", 26, Navy);
            Place(message.rectTransform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(0,35), new Vector2(500,90));
            var next = Button(panel, hasNext ? "Next lesson" : "Choose a lesson", Gold, () => NextPressed?.Invoke());
            Place(next.GetComponent<RectTransform>(), new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,100 + homeSpace), new Vector2(400,62));
            var replay = Button(panel, "Play again", Cream, () => ReplayPressed?.Invoke());
            Place(replay.GetComponent<RectTransform>(), new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,26 + homeSpace), new Vector2(400,54));
            AddDialogHomeButton(panel,20);
        }

        public void ShowError(string message)
        {
            OpenDialog();
            var panel = DialogCard("CHECK COURSE CONFIGURATION", new Vector2(620,600));
            var label = Label(panel,"Configuration errors",message,21,Navy);
            Stretch(label.rectTransform); label.rectTransform.offsetMin = new Vector2(28,28); label.rectTransform.offsetMax = new Vector2(-28,-90);
            label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 21;
        }

        private void OpenDialog()
        {
            foreach (Transform child in modal) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            modal.gameObject.SetActive(true); Flashlight.InputEnabled = false;
        }
        private void CloseDialog()
        {
            modal.gameObject.SetActive(false); Flashlight.ResetLight(); Flashlight.InputEnabled = true;
        }
        private RectTransform DialogCard(string title, Vector2 size)
        {
            float width = Mathf.Min(size.x, safe.rect.width - 32);
            float height = Mathf.Min(size.y, safe.rect.height - 32);
            var panel = Panel(modal,"Dialog card",Color.white).rectTransform;
            Place(panel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,height));
            var heading = Label(panel,"Heading",title,25,Navy); heading.fontStyle = FontStyles.Bold;
            Place(heading.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-24),new Vector2(width-40,44));
            return panel;
        }

        private void LateUpdate() => Layout(false);
        private void Layout(bool force)
        {
            if (safe == null) return;
            var size = new Vector2(Screen.width,Screen.height);
            if (!force && size == previousSize && Screen.safeArea == previousSafeArea) return;
            previousSize = size; previousSafeArea = Screen.safeArea;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1,size.x),area.yMin / Mathf.Max(1,size.y));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1,size.x),area.yMax / Mathf.Max(1,size.y));
            Canvas.ForceUpdateCanvases();
            float width = Mathf.Max(200,Mathf.Min(760,safe.rect.width - 32));
            content.sizeDelta = new Vector2(width,0);
            card.sizeDelta = new Vector2(width,112);
            wordLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(80,width-130),58);
            lessonLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(80,width-150),34);
            feedback.rectTransform.sizeDelta = new Vector2(Mathf.Max(80,width-260),28);
            instruction.rectTransform.sizeDelta = new Vector2(width,28);
            instruction.enableAutoSizing = true; instruction.fontSizeMin = 11; instruction.fontSizeMax = 17;
            field.offsetMin = new Vector2(0,homeNavigationEnabled ? 122 : 92); field.offsetMax = new Vector2(0,-202);
            Canvas.ForceUpdateCanvases();
            float side = Mathf.Min(field.rect.width,field.rect.height);
            foreach (var view in objects) view.Rect.sizeDelta = Vector2.one * (side * view.Placement.size);
            // Fit open dialogs after a device rotation as well.
            foreach (RectTransform dialog in modal)
                dialog.sizeDelta = new Vector2(Mathf.Min(dialog.sizeDelta.x,safe.rect.width-32),Mathf.Min(dialog.sizeDelta.y,safe.rect.height-32));
        }

        private RectTransform Rect(Transform parent,string name)
        {
            var obj = new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false);
            return obj.GetComponent<RectTransform>();
        }
        private Image Panel(Transform parent,string name,Color color)
        {
            var rect = Rect(parent,name); var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            return image;
        }
        private TextMeshProUGUI Label(Transform parent,string name,string text,float size,Color color)
        {
            var rect = Rect(parent,name); var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }
        private Button Button(Transform parent,string text,Color color,UnityAction action,Color? textColor = null)
        {
            var image = Panel(parent,text + " button",color); var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.onClick.AddListener(action);
            var label = Label(image.transform,"Label",text,22,textColor ?? Navy); Stretch(label.rectTransform);
            label.enableAutoSizing = true; label.fontSizeMin = 13; label.fontSizeMax = 22;
            return button;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static void Place(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private void OnDestroy() => art.Dispose();
    }
}
