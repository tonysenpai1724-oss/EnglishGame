using System;
using System.Collections.Generic;

namespace EnglishLearning
{
    /// <summary>Serializable progress keyed by stable IDs, never by catalog ordering.</summary>
    [Serializable]
    public sealed class CourseProgress
    {
        public int version = 1;
        public string lastTopicId;
        public List<TopicProgress> topics = new List<TopicProgress>();

        [Serializable]
        public sealed class TopicProgress
        {
            public string topicId;
            public string lastLessonId;
            public List<string> completedLessonIds = new List<string>();
        }

        private TopicProgress Find(string topicId) => topics?.Find(item => item != null && item.topicId == topicId);
        private TopicProgress GetOrCreate(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId)) throw new ArgumentException("A topic ID is required.");
            var entry = Find(topicId);
            if (entry != null) return entry;
            if (topics == null) topics = new List<TopicProgress>();
            entry = new TopicProgress { topicId = topicId };
            topics.Add(entry);
            return entry;
        }
        public string LastLesson(string topicId) => Find(topicId)?.lastLessonId;
        public bool IsCompleted(string topicId, string lessonId) => Find(topicId)?.completedLessonIds?.Contains(lessonId) == true;
        public int CompletedCount(string topicId, IEnumerable<string> availableLessonIds)
        {
            int count = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in availableLessonIds)
                if (seen.Add(id) && IsCompleted(topicId,id)) count++;
            return count;
        }
        public void SelectTopic(string topicId)
        {
            GetOrCreate(topicId);
            lastTopicId = topicId;
        }
        public void OpenLesson(string topicId, string lessonId)
        {
            if (string.IsNullOrWhiteSpace(lessonId)) throw new ArgumentException("A lesson ID is required.");
            SelectTopic(topicId);
            GetOrCreate(topicId).lastLessonId = lessonId;
        }
        public void CompleteLesson(string topicId, string lessonId)
        {
            OpenLesson(topicId,lessonId);
            var entry = GetOrCreate(topicId);
            if (entry.completedLessonIds == null) entry.completedLessonIds = new List<string>();
            if (!entry.completedLessonIds.Contains(lessonId)) entry.completedLessonIds.Add(lessonId);
        }
    }
}
