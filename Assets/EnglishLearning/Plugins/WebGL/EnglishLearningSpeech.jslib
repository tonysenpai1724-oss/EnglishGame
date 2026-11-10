mergeInto(LibraryManager.library, {
  EnglishLearningSpeak: function (textPointer) {
    if (!window.speechSynthesis || !window.SpeechSynthesisUtterance) return 0;
    var text = UTF8ToString(textPointer);
    window.speechSynthesis.cancel();
    var utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US';
    utterance.rate = 0.8;
    var voices = window.speechSynthesis.getVoices();
    var voice = voices.filter(function (v) { return v.lang === 'en-US'; })[0] ||
                voices.filter(function (v) { return /^en[-_]/i.test(v.lang); })[0];
    if (voice) utterance.voice = voice;
    window.speechSynthesis.speak(utterance);
    return 1;
  },
  EnglishLearningStopSpeech: function () {
    if (window.speechSynthesis) window.speechSynthesis.cancel();
  }
});
