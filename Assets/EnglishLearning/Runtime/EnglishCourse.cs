using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishLearning
{
    public enum DemoObjectIcon { Apple, Banana, Orange, Strawberry, Pear, Book, Pencil, Cup, Key, Ball }

    [Serializable]
    public sealed class VocabularyWord
    {
        [Tooltip("Stable unique ID used by lesson placements, e.g. apple. Changing it requires updating placements.")]
        public string id;
        public string english;
        [Tooltip("Optional local recording. Takes priority over device/browser speech.")]
        public AudioClip pronunciation;
        [Tooltip("Your own artwork. If empty, the demo illustration below is used.")]
        public Sprite sprite;
        public DemoObjectIcon demoIcon;
    }

    [Serializable]
    public sealed class LessonObjectPlacement
    {
        public string wordId;
        [Tooltip("Position within the play field: (0,0) bottom-left, (1,1) top-right.")]
        public Vector2 position = new Vector2(.5f, .5f);
        [Tooltip("Size as a fraction of the play field's shorter side.")]
        [Range(.08f, .35f)] public float size = .2f;
        [Range(-180, 180)] public float rotation;
    }

    [Serializable]
    public sealed class EnglishLesson
    {
        public string id;
        public string title;
        public Color background = new Color(.86f, .91f, .85f);
        public bool shuffleWords;
        public List<LessonObjectPlacement> objects = new List<LessonObjectPlacement>();
    }

    [Serializable]
    public sealed class EnglishTopic
    {
        [Tooltip("Stable topic ID. Used to save progress independently of the topic's display order.")]
        public string id;
        public string title;
        [TextArea] public string description;
        [Tooltip("Ordered IDs from the lesson library below. A lesson may be reused across topics.")]
        public List<string> lessonIds = new List<string>();
    }

    [CreateAssetMenu(menuName = "English Learning/Course", fileName = "EnglishCourse")]
    public sealed class EnglishCourse : ScriptableObject
    {
        public string title = "Word Explorer";
        [Header("Gameplay")]
        [Tooltip("Radius as a fraction of the play field's shorter side.")]
        [Range(.1f, .4f)] public float flashlightRadius = .22f;
        [Range(0, .08f)] public float flashlightFeather = .025f;
        public bool autoPronounce = true;
        public bool rememberLesson = true;
        [Tooltip("Use a different key for each course. Topic progress is stored as IDs at this key + .Topics.v1; the old integer save is migrated on first use.")]
        public string progressKey = "EnglishLearning.Demo.LastLesson";
        [Header("Content")]
        public List<VocabularyWord> vocabulary = new List<VocabularyWord>();
        [Tooltip("Shared lesson library. Topics reference lessons by ID, not array index.")]
        public List<EnglishLesson> lessons = new List<EnglishLesson>();
        public List<EnglishTopic> topics = new List<EnglishTopic>();
        public string TopicProgressKey => progressKey + ".Topics.v1";

        public int FindLessonIndex(string id) => lessons.FindIndex(lesson => lesson != null && lesson.id == id);
        public int FindTopicIndex(string id) => topics.FindIndex(topic => topic != null && topic.id == id);
        public int FindTopicForLesson(string lessonId, int preferred = -1)
        {
            if (preferred >= 0 && preferred < topics.Count && topics[preferred].lessonIds.Contains(lessonId)) return preferred;
            return topics.FindIndex(topic => topic != null && topic.lessonIds != null && topic.lessonIds.Contains(lessonId));
        }
        public int NextLessonIndex(int topicIndex, string lessonId)
        {
            if (topicIndex < 0 || topicIndex >= topics.Count) return -1;
            var ids = topics[topicIndex].lessonIds;
            int current = ids.IndexOf(lessonId);
            return current >= 0 && current + 1 < ids.Count ? FindLessonIndex(ids[current + 1]) : -1;
        }

        public VocabularyWord FindWord(string id) => vocabulary.Find(word => word != null && word.id == id);

        public List<string> ValidateContent()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (vocabulary == null || lessons == null || topics == null)
            {
                errors.Add("Vocabulary, lesson library, and topics must not be null.");
                return errors;
            }
            foreach (var word in vocabulary)
            {
                if (word == null || string.IsNullOrWhiteSpace(word.id) || string.IsNullOrWhiteSpace(word.english))
                    errors.Add("Each vocabulary item needs an ID and English text.");
                else if (!ids.Add(word.id)) errors.Add("Duplicate word ID: " + word.id);
            }
            if (lessons.Count == 0) errors.Add("Add at least one lesson.");
            var lessonIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var lesson in lessons)
            {
                if (lesson == null) { errors.Add("A lesson is null."); continue; }
                if (string.IsNullOrWhiteSpace(lesson.id) || !lessonIds.Add(lesson.id))
                    errors.Add("Lesson IDs must be non-empty and unique: " + lesson.id);
                if (string.IsNullOrWhiteSpace(lesson.title)) errors.Add("Lesson needs a title: " + lesson.id);
                if (lesson.objects == null || lesson.objects.Count == 0)
                { errors.Add("Lesson needs objects: " + lesson.id); continue; }
                foreach (var item in lesson.objects)
                {
                    if (item == null) { errors.Add(lesson.id + ": null placement."); continue; }
                    if (string.IsNullOrEmpty(item.wordId) || !ids.Contains(item.wordId))
                        errors.Add(lesson.id + ": unknown word ID '" + item.wordId + "'.");
                    float margin = item.size * .72f; // Conservative allowance for rotated artwork.
                    if (!float.IsFinite(item.size) || item.size < .08f || item.size > .35f ||
                        !float.IsFinite(item.position.x) || !float.IsFinite(item.position.y) ||
                        !float.IsFinite(item.rotation) ||
                        item.position.x < margin || item.position.x > 1 - margin ||
                        item.position.y < margin || item.position.y > 1 - margin)
                        errors.Add(lesson.id + ": placement must stay inside the field with size .08–.35: " + item.wordId);
                }
                for (int i = 0; i < lesson.objects.Count; i++)
                for (int j = i + 1; j < lesson.objects.Count; j++)
                {
                    var a = lesson.objects[i];
                    var b = lesson.objects[j];
                    if (a == null || b == null) continue;
                    // Reject fully overlapping hit areas that would make a target unreachable.
                    if ((a.position - b.position).sqrMagnitude < .0001f)
                        errors.Add(lesson.id + ": objects must not share the same position.");
                }
            }
            if (topics.Count == 0) errors.Add("Add at least one topic and assign lesson IDs to it.");
            var topicIds = new HashSet<string>(StringComparer.Ordinal);
            var assigned = new HashSet<string>(StringComparer.Ordinal);
            foreach (var topic in topics)
            {
                if (topic == null) { errors.Add("A topic is null."); continue; }
                if (string.IsNullOrWhiteSpace(topic.id) || !topicIds.Add(topic.id))
                    errors.Add("Topic IDs must be non-empty and unique: " + topic.id);
                if (string.IsNullOrWhiteSpace(topic.title)) errors.Add("Topic needs a title: " + topic.id);
                if (topic.lessonIds == null || topic.lessonIds.Count == 0)
                { errors.Add("Topic needs at least one lesson: " + topic.id); continue; }
                var unique = new HashSet<string>(StringComparer.Ordinal);
                foreach (string id in topic.lessonIds)
                {
                    if (string.IsNullOrWhiteSpace(id) || !lessonIds.Contains(id))
                        errors.Add(topic.id + ": unknown lesson ID '" + id + "'.");
                    else assigned.Add(id);
                    if (!unique.Add(id)) errors.Add(topic.id + ": duplicate lesson ID '" + id + "'.");
                }
            }
            foreach (string id in lessonIds)
                if (!assigned.Contains(id)) errors.Add("Lesson is not assigned to any topic: " + id);
            if (!float.IsFinite(flashlightRadius) || flashlightRadius < .1f || flashlightRadius > .4f ||
                !float.IsFinite(flashlightFeather) || flashlightFeather < 0 || flashlightFeather > .08f)
                errors.Add("Flashlight radius/feather are outside their supported ranges.");
            if (rememberLesson && string.IsNullOrWhiteSpace(progressKey)) errors.Add("Set a progress save key.");
            return errors;
        }

        [ContextMenu("Validate course")]
        private void ValidateCourse()
        {
            var errors = ValidateContent();
            if (errors.Count == 0) Debug.Log("English course is valid.", this);
            else Debug.LogError(string.Join("\n", errors), this);
        }
    }
}
