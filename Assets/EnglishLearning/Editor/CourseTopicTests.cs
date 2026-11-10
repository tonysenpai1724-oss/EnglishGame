using System;
using EnglishLearning;
using UnityEngine;

public static class CourseTopicTests
{
    public static void RunAll(EnglishCourse source)
    {
        Check(source.topics.Count >= 3,"Demo needs its topic groups.");
        foreach (var topic in source.topics)
        {
            Check(topic.lessonIds.Count >= 2,"Each demo topic should contain multiple lessons.");
            int owner = source.FindTopicIndex(topic.id);
            for (int i = 0; i < topic.lessonIds.Count; i++)
            {
                int expected = i+1 < topic.lessonIds.Count ? source.FindLessonIndex(topic.lessonIds[i+1]) : -1;
                Check(source.NextLessonIndex(owner,topic.lessonIds[i]) == expected,"Next must follow the topic order, not library order.");
            }
        }
        var course = UnityEngine.Object.Instantiate(source);
        try
        {
            var p = new CourseProgress();
            string topicId = course.topics[0].id, lessonId = course.topics[0].lessonIds[0];
            p.CompleteLesson(topicId,lessonId);
            var restored = JsonUtility.FromJson<CourseProgress>(JsonUtility.ToJson(p));
            course.topics.Reverse(); course.lessons.Reverse();
            Check(restored.IsCompleted(topicId,lessonId),"Serialization or reorder lost stable-ID progress.");
            Check(course.FindLessonIndex(lessonId) >= 0 && course.FindTopicIndex(topicId) >= 0,"ID lookup broke after reorder.");
            course.topics[0].lessonIds.Add("missing-lesson");
            Check(course.ValidateContent().Exists(e => e.Contains("unknown lesson ID")),"Missing lesson reference was not rejected.");
            course.topics[0].lessonIds.Remove("missing-lesson");
            course.topics[0].lessonIds.Add(course.topics[0].lessonIds[0]);
            Check(course.ValidateContent().Exists(e => e.Contains("duplicate lesson ID")),"Duplicate lesson reference was not rejected.");
            course.topics[0].lessonIds.RemoveAt(course.topics[0].lessonIds.Count-1);
            string original = course.topics[1].id;
            course.topics[1].id = course.topics[0].id;
            Check(course.ValidateContent().Exists(e => e.Contains("Topic IDs")),"Duplicate topic ID was not rejected.");
            course.topics[1].id = original;
            course.topics[0].lessonIds.Clear();
            Check(course.ValidateContent().Exists(e => e.Contains("Topic needs at least one lesson")),"Empty topic was not rejected.");
            Check(course.ValidateContent().Exists(e => e.Contains("not assigned to any topic")),"Unreachable lesson was not rejected.");
            course.topics.Clear();
            Check(course.ValidateContent().Exists(e => e.Contains("Add at least one topic")),"Missing topics were not rejected.");
            // Large catalogs and lesson reuse must not require gameplay changes.
            var allIds = course.lessons.ConvertAll(lesson => lesson.id);
            for (int i = 0; i < 100; i++)
            {
                var template = course.lessons[0];
                string id = "scale-lesson-" + i;
                course.lessons.Add(new EnglishLesson { id = id, title = "Scale lesson " + i, objects = template.objects });
                allIds.Add(id);
            }
            for (int i = 0; i < 60; i++)
                course.topics.Add(new EnglishTopic { id = "scale-topic-" + i, title = "Scale topic " + i,
                    lessonIds = new System.Collections.Generic.List<string>(allIds) });
            Check(course.ValidateContent().Count == 0,"Large catalog / lesson reuse was rejected.");
            Check(course.NextLessonIndex(59,allIds[allIds.Count-2]) == course.FindLessonIndex(allIds[allIds.Count-1]),"Large catalog navigation failed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(course); }
    }
    private static void Check(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
