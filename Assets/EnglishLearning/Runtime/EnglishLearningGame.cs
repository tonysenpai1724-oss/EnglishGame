using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EnglishLearning
{
    [DisallowMultipleComponent]
    public sealed class EnglishLearningGame : MonoBehaviour
    {
        [SerializeField] private EnglishCourse course;
        [SerializeField, Min(0)] private int startingLesson = 0;
        private EnglishGameView view;
        private IWordPronouncer pronouncer;
        private LessonSession session;
        private int lessonIndex;
        private float feedbackUntil;
        public bool IsInitialized { get; private set; }
        public string InitializationError { get; private set; }
        public event System.Action HomeRequested;
        private bool returningHome;
        public LessonSession Session => session;
        public EnglishCourse Course => course;
        public int LessonIndex => lessonIndex;

        private void Start()
        {
            if (course == null) course = Resources.Load<EnglishCourse>("EnglishLearning/DemoCourse");
            EnsureSceneServices();
            view = gameObject.AddComponent<EnglishGameView>();
            view.Build();
            view.HomePressed += RequestHome;
            if (course == null)
            {
                InitializationError = "Assign an EnglishCourse to the game controller.\nCreate one via Assets > Create > English Learning > Course.";
                view.ShowError(InitializationError);
                return;
            }
            List<string> errors = course.ValidateContent();
            if (errors.Count > 0)
            {
                InitializationError = string.Join("\n",errors);
                view.ShowError(InitializationError);
                Debug.LogError("English course configuration:\n" + string.Join("\n",errors),course);
                return;
            }
            pronouncer = gameObject.AddComponent<WordAudioPlayer>();
            view.Flashlight.Selected += SelectAt;
            view.SpeakerPressed += Pronounce;
            view.LessonsPressed += OpenLessons;
            view.RestartPressed += Replay;
            view.LessonChosen += LoadLesson;
            view.NextPressed += Next;
            view.ReplayPressed += Replay;
            int saved = course.rememberLesson ? PlayerPrefs.GetInt(course.progressKey,startingLesson) : startingLesson;
            LoadLesson(Mathf.Clamp(saved,0,course.lessons.Count - 1));
            IsInitialized = true;
        }

        private void EnsureSceneServices()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("English Learning EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                events.transform.SetParent(transform,false);
            }
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                var listener = new GameObject("English Learning Audio Listener",typeof(AudioListener));
                listener.transform.SetParent(transform,false);
            }
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("English Learning Camera",typeof(Camera));
                cameraObject.transform.SetParent(transform,false);
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 0; // All gameplay is rendered by the overlay canvas.
            }
        }

        /// <summary>The application shell supplies navigation; standalone gameplay has no shell dependency.</summary>
        public void EnableHomeNavigation()
        {
            view.SetHomeNavigation(true);
        }

        public void RequestHome()
        {
            if (returningHome || HomeRequested == null) return;
            returningHome = true;
            pronouncer?.Stop();
            view.Flashlight.InputEnabled = false;
            HomeRequested.Invoke();
        }

        public void LoadLesson(int index)
        {
            if (index < 0 || index >= course.lessons.Count) return;
            pronouncer.Stop();
            lessonIndex = index;
            var lesson = course.lessons[index];
            var ids = lesson.objects.ConvertAll(item => item.wordId);
            session = new LessonSession(ids,lesson.shuffleWords,Random.Range(0,int.MaxValue));
            view.ShowLesson(lesson,course.flashlightRadius,course.flashlightFeather);
            view.SetObjectSprites(course);
            feedbackUntil = 0;
            RefreshWord();
            if (course.rememberLesson)
            {
                PlayerPrefs.SetInt(course.progressKey,index);
                PlayerPrefs.Save();
            }
        }

        private void SelectAt(Vector2 point)
        {
            if (session == null || session.IsComplete) return;
            string selected = view.HitObject(point);
            if (selected == null) return;
            if (!session.TrySelect(selected))
            {
                view.SetFeedback("Try another object.");
                feedbackUntil = Time.unscaledTime + 1.8f;
                return;
            }
            view.MarkFound(selected);
            pronouncer.Stop();
            view.SetFeedback("Well done!",true);
            feedbackUntil = Time.unscaledTime + 1.5f;
            RefreshWord();
            if (session.IsComplete)
                view.ShowComplete(course.lessons[lessonIndex].title,lessonIndex + 1 < course.lessons.Count);
        }

        private void RefreshWord()
        {
            view.SetWord(session.IsComplete ? "Complete!" : course.FindWord(session.CurrentWordId).english,
                session.CompletedCount,session.TotalCount);
            if (!session.IsComplete && course.autoPronounce) Pronounce();
        }

        private void Pronounce()
        {
            if (session == null || session.IsComplete) return;
            if (!pronouncer.Speak(course.FindWord(session.CurrentWordId)))
            {
                view.SetFeedback("Add a pronunciation clip for this word.");
                feedbackUntil = Time.unscaledTime + 3;
            }
        }

        private void OpenLessons() { pronouncer.Stop(); view.ShowLessons(course,lessonIndex); }
        private void Replay() => LoadLesson(lessonIndex);
        private void Next()
        {
            if (lessonIndex + 1 < course.lessons.Count) LoadLesson(lessonIndex + 1);
            else OpenLessons();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) RequestHome();
            if (feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil)
            { feedbackUntil = 0; view.SetFeedback(""); }
        }
        private void OnDisable() => pronouncer?.Stop();
    }
}
