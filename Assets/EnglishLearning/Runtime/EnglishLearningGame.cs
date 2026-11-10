using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EnglishLearning
{
    public enum EnglishGameScreen { Topics, Lessons, Playing, Complete, Error }

    [DisallowMultipleComponent]
    public sealed class EnglishLearningGame : MonoBehaviour
    {
        [SerializeField] private EnglishCourse course;
        [SerializeField, Min(0)] private int startingLesson = 0;
        private EnglishGameView view;
        private IWordPronouncer pronouncer;
        private LessonSession session;
        private CourseProgress progress;
        private int lessonIndex = -1, topicIndex = -1, browsingTopicIndex;
        private float feedbackUntil;
        private bool returningHome;
        public bool IsInitialized { get; private set; }
        public string InitializationError { get; private set; }
        public event System.Action HomeRequested;
        public LessonSession Session => session;
        public EnglishCourse Course => course;
        public CourseProgress Progress => progress;
        public int LessonIndex => lessonIndex;
        public int TopicIndex => topicIndex;
        public int BrowsingTopicIndex => browsingTopicIndex;
        public EnglishGameScreen Screen { get; private set; }

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
                Screen = EnglishGameScreen.Error;
                view.ShowError(InitializationError);
                return;
            }
            List<string> errors = course.ValidateContent();
            if (errors.Count > 0)
            {
                InitializationError = string.Join("\n",errors);
                Screen = EnglishGameScreen.Error;
                view.ShowError(InitializationError);
                Debug.LogError("English course configuration:\n" + InitializationError,course);
                return;
            }
            pronouncer = gameObject.AddComponent<WordAudioPlayer>();
            view.Flashlight.Selected += SelectAt;
            view.SpeakerPressed += Pronounce;
            view.TopicsPressed += OpenTopics;
            view.TopicChosen += OpenTopic;
            view.BackToTopicsPressed += OpenTopics;
            view.ResumePressed += ResumeLesson;
            view.RestartPressed += Replay;
            view.LessonChosen += LoadLesson;
            view.NextPressed += Next;
            view.ReplayPressed += Replay;
            RestoreProgress();
            browsingTopicIndex = Mathf.Max(0,course.FindTopicIndex(progress.lastTopicId));
            OpenTopics(); // Enter through topic selection; don't start or pronounce a hidden lesson.
            IsInitialized = true;
        }

        private void RestoreProgress()
        {
            progress = new CourseProgress();
            if (!course.rememberLesson) return;
            if (PlayerPrefs.HasKey(course.TopicProgressKey))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<CourseProgress>(PlayerPrefs.GetString(course.TopicProgressKey));
                    if (loaded != null && loaded.version == 1) { progress = loaded; return; }
                }
                catch (System.ArgumentException) { Debug.LogWarning("Unreadable topic progress; using a fresh selection.",this); }
            }
            // Preserve the old last-opened lesson without guessing which lessons were completed.
            int old = Mathf.Clamp(PlayerPrefs.GetInt(course.progressKey,startingLesson),0,course.lessons.Count - 1);
            string lessonId = course.lessons[old].id;
            int topic = course.FindTopicForLesson(lessonId);
            if (topic >= 0) progress.OpenLesson(course.topics[topic].id,lessonId);
        }
        private void SaveProgress()
        {
            if (!course.rememberLesson) return;
            PlayerPrefs.SetString(course.TopicProgressKey,JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
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
                camera.cullingMask = 0;
            }
        }

        public void EnableHomeNavigation()
        {
            view.SetHomeNavigation(true);
            // The shell attaches after Start; refresh any already-open picker to include Home.
            if (Screen == EnglishGameScreen.Topics) OpenTopics();
            else if (Screen == EnglishGameScreen.Lessons) OpenTopic(browsingTopicIndex);
        }
        public void RequestHome()
        {
            if (returningHome || HomeRequested == null) return;
            returningHome = true;
            pronouncer?.Stop();
            view.Flashlight.InputEnabled = false;
            HomeRequested.Invoke();
        }

        public void OpenTopics()
        {
            if (returningHome) return;
            pronouncer.Stop();
            Screen = EnglishGameScreen.Topics;
            view.ShowTopics(course,progress,browsingTopicIndex,session != null);
        }
        public void OpenTopic(int index)
        {
            if (returningHome || index < 0 || index >= course.topics.Count) return;
            pronouncer.Stop();
            browsingTopicIndex = index;
            progress.SelectTopic(course.topics[index].id);
            SaveProgress();
            Screen = EnglishGameScreen.Lessons;
            view.ShowLessons(course,index,progress,session != null);
        }
        public void LoadLesson(int index)
        {
            if (returningHome || index < 0 || index >= course.lessons.Count) return;
            string id = course.lessons[index].id;
            int owner = course.FindTopicForLesson(id,browsingTopicIndex);
            if (owner < 0) return;
            pronouncer.Stop();
            topicIndex = browsingTopicIndex = owner;
            lessonIndex = index;
            var lesson = course.lessons[index];
            session = new LessonSession(lesson.objects.ConvertAll(item => item.wordId),lesson.shuffleWords,Random.Range(0,int.MaxValue));
            Screen = EnglishGameScreen.Playing;
            view.ShowLesson(lesson,course.flashlightRadius,course.flashlightFeather);
            view.SetObjectSprites(course);
            view.SetLessonTitle(course.topics[topicIndex].title + " / " + lesson.title);
            feedbackUntil = 0;
            progress.OpenLesson(course.topics[topicIndex].id,lesson.id);
            SaveProgress();
            RefreshWord();
        }
        private void ResumeLesson()
        {
            if (session == null || returningHome) return;
            browsingTopicIndex = topicIndex; // Browsing another topic must not change active Next/Replay context.
            progress.OpenLesson(course.topics[topicIndex].id,course.lessons[lessonIndex].id);
            SaveProgress();
            if (session.IsComplete) ShowComplete();
            else { Screen = EnglishGameScreen.Playing; view.ResumeGame(); }
        }

        private void SelectAt(Vector2 point)
        {
            if (returningHome || Screen != EnglishGameScreen.Playing || session == null || session.IsComplete) return;
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
            if (!session.IsComplete) return;
            progress.CompleteLesson(course.topics[topicIndex].id,course.lessons[lessonIndex].id);
            SaveProgress();
            ShowComplete();
        }
        private void ShowComplete()
        {
            Screen = EnglishGameScreen.Complete;
            view.ShowComplete(course.lessons[lessonIndex].title,course.NextLessonIndex(topicIndex,course.lessons[lessonIndex].id) >= 0);
        }
        private void RefreshWord()
        {
            view.SetWord(session.IsComplete ? "Complete!" : course.FindWord(session.CurrentWordId).english,session.CompletedCount,session.TotalCount);
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
        private void Replay()
        {
            if (session == null) return;
            browsingTopicIndex = topicIndex;
            LoadLesson(lessonIndex);
        }
        private void Next()
        {
            if (session == null || !session.IsComplete) return;
            int next = course.NextLessonIndex(topicIndex,course.lessons[lessonIndex].id);
            if (next < 0) OpenTopics();
            else { browsingTopicIndex = topicIndex; LoadLesson(next); }
        }
        public void NavigateBack()
        {
            if (!IsInitialized || returningHome) return;
            if (Screen == EnglishGameScreen.Lessons) OpenTopics();
            else if (Screen == EnglishGameScreen.Topics)
            {
                if (session != null) ResumeLesson(); else RequestHome();
            }
            else OpenTopic(topicIndex);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) NavigateBack();
            if (feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil)
            { feedbackUntil = 0; view.SetFeedback(""); }
        }
        private void OnDisable() => pronouncer?.Stop();
    }
}
