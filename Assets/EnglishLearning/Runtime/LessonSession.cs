using System;
using System.Collections.Generic;

namespace EnglishLearning
{
    /// <summary>Pure gameplay state. No scene, UI, audio, or persistence dependencies.</summary>
    public sealed class LessonSession
    {
        private readonly List<string> targets = new List<string>();
        private readonly HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
        public int CompletedCount { get; private set; }
        public int TotalCount => targets.Count;
        public bool IsComplete => CompletedCount == TotalCount;
        public string CurrentWordId => IsComplete ? null : targets[CompletedCount];
        public event Action Changed;
        public event Action Completed;

        public LessonSession(IEnumerable<string> wordIds, bool shuffle = false, int seed = 0)
        {
            if (wordIds == null) throw new ArgumentNullException(nameof(wordIds));
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in wordIds)
            {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Word IDs cannot be empty.");
                if (unique.Add(id)) targets.Add(id);
            }
            if (targets.Count == 0) throw new ArgumentException("A lesson needs at least one word.");
            if (!shuffle) return;
            var random = new Random(seed);
            for (int i = targets.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string temporary = targets[i];
                targets[i] = targets[j];
                targets[j] = temporary;
            }
        }

        public bool HasFound(string wordId) => found.Contains(wordId);

        public bool TrySelect(string wordId)
        {
            if (IsComplete || !string.Equals(wordId, CurrentWordId, StringComparison.Ordinal)) return false;
            found.Add(wordId);
            CompletedCount++;
            Changed?.Invoke();
            if (IsComplete) Completed?.Invoke();
            return true;
        }
    }
}
