using System.Runtime.InteropServices;
using UnityEngine;

namespace EnglishLearning
{
    public interface IWordPronouncer
    {
        bool Speak(VocabularyWord word);
        void Stop();
    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class WordAudioPlayer : MonoBehaviour, IWordPronouncer
    {
        private AudioSource source;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int EnglishLearningSpeak(string text);
        [DllImport("__Internal")] private static extern void EnglishLearningStopSpeech();
#endif
        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
        }

        public bool Speak(VocabularyWord word)
        {
            Stop();
            if (word == null) return false;
            if (word.pronunciation != null)
            {
                source.clip = word.pronunciation;
                source.Play();
                return true;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            return EnglishLearningSpeak(word.english) != 0;
#else
            return false; // Assign a local clip for portable, offline pronunciation.
#endif
        }

        public void Stop()
        {
            if (source != null) source.Stop();
#if UNITY_WEBGL && !UNITY_EDITOR
            EnglishLearningStopSpeech();
#endif
        }

        private void OnDisable() => Stop();
    }
}
