using System;
using EnglishLearning;

/// <summary>Pure C# progress regression tests, also runnable without Unity.</summary>
public static class CourseProgressTests
{
    private static int assertions;
    public static int Main()
    {
        try { Console.WriteLine("PASS: " + RunAll() + " topic progress assertions."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    public static int RunAll()
    {
        assertions = 0;
        var p = new CourseProgress();
        Check(!p.IsCompleted("fruit","a") && p.LastLesson("fruit") == null,"Fresh progress is not empty.");
        p.SelectTopic("fruit");
        Check(p.lastTopicId == "fruit" && p.topics.Count == 1,"Selecting a topic failed.");
        p.OpenLesson("fruit","a");
        Check(p.LastLesson("fruit") == "a" && !p.IsCompleted("fruit","a"),"Opening a lesson incorrectly marked it complete.");
        p.CompleteLesson("fruit","a");
        p.CompleteLesson("fruit","a");
        Check(p.IsCompleted("fruit","a") && p.topics[0].completedLessonIds.Count == 1,"Completion was not idempotent.");
        p.OpenLesson("fruit","b");
        Check(p.LastLesson("fruit") == "b" && p.IsCompleted("fruit","a"),"Changing lesson erased completion.");
        p.CompleteLesson("daily","a");
        Check(p.lastTopicId == "daily" && !p.IsCompleted("daily","b"),"Topic progress leaked.");
        Check(p.LastLesson("fruit") == "b" && p.LastLesson("daily") == "a","Last played is not per topic.");
        Check(p.CompletedCount("fruit",new[] {"a","b"}) == 1,"Completed count incorrect.");
        Check(p.CompletedCount("fruit",new[] {"b","a"}) == 1,"Reordering changed progress.");
        Check(p.CompletedCount("fruit",new[] {"a","a"}) == 1,"Duplicate references inflated count.");
        Check(p.CompletedCount("fruit",new[] {"b","c"}) == 0,"Deleted lessons remain in displayed count.");
        p.CompleteLesson("fruit","b");
        Check(p.CompletedCount("fruit",new[] {"a","b","c"}) == 2,"Adding a new lesson broke progress.");
        Check(!p.IsCompleted("Fruit","a"),"IDs must be case sensitive.");
        p.topics = null;
        Check(p.LastLesson("fruit") == null && !p.IsCompleted("fruit","a"),"Null serialized progress list crashed.");
        p.CompleteLesson("fruit","a");
        Check(p.IsCompleted("fruit","a"),"Null list could not be recovered.");
        p.topics[0].completedLessonIds = null;
        p.CompleteLesson("fruit","b");
        Check(p.IsCompleted("fruit","b"),"Null completed list could not be recovered.");
        Throws(() => p.SelectTopic(" "));
        Throws(() => p.OpenLesson("fruit",null));
        return assertions;
    }
    private static void Check(bool condition,string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws(Action action)
    {
        assertions++;
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Expected ArgumentException.");
    }
}
