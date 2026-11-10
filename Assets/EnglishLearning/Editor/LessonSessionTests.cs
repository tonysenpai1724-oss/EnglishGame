using System;
using System.Collections.Generic;
using EnglishLearning;

/// <summary>Also runnable without Unity: compile this file together with LessonSession.cs.</summary>
public static class LessonSessionTests
{
    private static int assertions;
    public static int Main()
    {
        try { Console.WriteLine("PASS: " + RunAll() + " lesson model assertions."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    public static int RunAll()
    {
        assertions = 0;
        Throws<ArgumentNullException>(() => new LessonSession(null));
        Throws<ArgumentException>(() => new LessonSession(new string[0]));
        Throws<ArgumentException>(() => new LessonSession(new[] {"apple"," "}));
        Throws<ArgumentException>(() => new LessonSession(new[] {"apple",null}));
        var session = new LessonSession(new[] {"apple","banana","apple"});
        Check(session.TotalCount == 2 && session.CurrentWordId == "apple","Targets must be unique and ordered.");
        int changed = 0, completed = 0;
        session.Changed += () => changed++;
        session.Completed += () => completed++;
        Check(!session.TrySelect("banana") && !session.TrySelect(null),"Wrong/null answers must not advance.");
        Check(session.CompletedCount == 0 && changed == 0,"Wrong selection mutated progress.");
        Check(session.TrySelect("apple") && session.HasFound("apple"),"Correct answer not marked found.");
        Check(session.CurrentWordId == "banana" && !session.TrySelect("apple"),"Found word accepted twice.");
        Check(session.TrySelect("banana") && session.IsComplete && session.CurrentWordId == null,"Lesson did not complete.");
        Check(!session.TrySelect("banana") && !session.TrySelect(null),"Completed session accepted input.");
        Check(changed == 2 && completed == 1,"Incorrect event counts.");
        Check(!new LessonSession(new[] {"apple"}).HasFound("apple"),"Replay inherited previous progress.");
        Check(!new LessonSession(new[] {"apple"}).TrySelect("Apple"),"IDs should be case-sensitive.");
        for (int seed = 0; seed < 64; seed++)
        {
            var a = new LessonSession(new[] {"a","b","c","d"},true,seed);
            var b = new LessonSession(new[] {"a","b","c","d"},true,seed);
            var seen = new HashSet<string>();
            while (!a.IsComplete)
            {
                string id = a.CurrentWordId;
                Check(id == b.CurrentWordId && seen.Add(id),"Shuffle must be deterministic and preserve unique targets.");
                Check(a.TrySelect(id) && b.TrySelect(id),"Shuffled target rejected.");
            }
            Check(seen.Count == 4,"Shuffle dropped a word.");
        }
        return assertions;
    }
    private static void Check(bool condition,string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        assertions++;
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
