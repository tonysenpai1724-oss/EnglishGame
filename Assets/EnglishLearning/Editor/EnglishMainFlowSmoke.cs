using System;
using System.IO;
using System.Linq;
using EnglishLearning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Explicit integration check that starts at Splash, not at the standalone lesson scene.</summary>
[InitializeOnLoad]
public static class EnglishMainFlowSmoke
{
    private const string Running = "EnglishLearning.MainFlowSmoke.Running";
    private const string Backup = "EnglishLearning.MainFlowSmoke.Backup";
    private const string Request = "Temp/EnglishLearning.main-flow.request";
    private const string Report = "Temp/EnglishLearning.main-flow.txt";
    private static int stage, checks;
    private static double deadline, nextCheck;
    private static long energyBefore;
    private static EnglishLearningGame game;
    private static EnglishGameView view;

    static EnglishMainFlowSmoke()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode) { stage = 0; checks = 0; deadline = EditorApplication.timeSinceStartup + 120; }
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Running,false)) return;
            // Restore only local resource/timer/course saves touched by this test, not unrelated player data.
            if (SessionState.GetBool(Backup,false))
            {
                CPlayerPrefs.SetString("player_resource",SessionState.GetString(Backup + ".resource",""));
                CPlayerPrefs.SetString("timer_system",SessionState.GetString(Backup + ".timer",""));
                string key = SessionState.GetString(Backup + ".key","");
                if (SessionState.GetBool(Backup + ".hasLesson",false)) PlayerPrefs.SetInt(key,SessionState.GetInt(Backup + ".lesson",0));
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
            SessionState.SetBool(Backup,false);
            SessionState.SetBool(Running,false);
        };
    }

    [MenuItem("Tools/English Learning/Run Main Flow Smoke Test")]
    public static void RequestSmoke()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        File.WriteAllText(Request,"run");
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request)) return;
            File.Delete(Request);
            try
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    Check(!SceneManager.GetSceneAt(i).isDirty,"Save open scenes before running this test.");
                Check(EditorBuildSettings.scenes.First(s => s.enabled).path == "Assets/Scenes/Splash.unity","Splash must be the build entry scene.");
                Check(Constant.SCENE_GAME_PLAY == "EnglishLearning","Home Play still targets legacy gameplay.");
                SessionState.SetBool(Backup,false);
                SessionState.SetBool(Running,true);
                File.WriteAllText(Report,"RUNNING: Splash -> Loading -> Home -> English lessons -> Home -> replay.");
                EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity");
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex) { Fail(ex); }
            return;
        }
        if (!SessionState.GetBool(Running,false)) return;
        try
        {
            Check(EditorApplication.timeSinceStartup < deadline,"Main flow timed out at stage " + stage + " in scene " + SceneManager.GetActiveScene().name);
            var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stage == 0)
            {
                if (manager == null || !manager.Initilized || manager.GameState != EGameState.Home || !UIManager.Instance.Initialized) return;
                Check(SceneManager.GetActiveScene().name == Constant.SCENE_MAIN_UI,"Boot did not reach Home.");
                var course = AssetDatabase.LoadAssetAtPath<EnglishCourse>(EnglishLearningTools.CoursePath);
                SessionState.SetString(Backup + ".resource",CPlayerPrefs.GetString("player_resource"));
                SessionState.SetString(Backup + ".timer",CPlayerPrefs.GetString("timer_system"));
                SessionState.SetString(Backup + ".key",course.progressKey);
                SessionState.SetBool(Backup + ".hasLesson",PlayerPrefs.HasKey(course.progressKey));
                SessionState.SetInt(Backup + ".lesson",PlayerPrefs.GetInt(course.progressKey));
                SessionState.SetBool(Backup,true);
                energyBefore = IPlayerResource.Instance.GetCommonResource(ECommonResource.Energy);
                Check(energyBefore >= 2,"This test needs at least 2 Energy; it will not grant or purchase resources.");
                ScreenCapture.CaptureScreenshot("Temp/EnglishLearning.main-home.png");
                Click(UnityEngine.Object.FindFirstObjectByType<HomePlayButton>().button);
                Check(IPlayerResource.Instance.GetCommonResource(ECommonResource.Energy) == energyBefore - 1,"Home Play did not charge exactly 1 Energy.");
                stage = 1; deadline = EditorApplication.timeSinceStartup + 40;
            }
            else if (stage == 1 || stage == 3)
            {
                if (manager == null || manager.GameState != EGameState.Gameplay) return;
                game = UnityEngine.Object.FindFirstObjectByType<EnglishLearningGame>();
                Check(game != null && game.IsInitialized,"Home Play did not initialize English gameplay.");
                Check(SceneManager.GetActiveScene().name == Constant.SCENE_GAME_PLAY && UIManager.Instance.Initialized,"Gameplay transition/UI initialization did not finish.");
                Check(UnityEngine.Object.FindFirstObjectByType<GameplayManager>() == null && UnityEngine.Object.FindFirstObjectByType<CandySort.CandySortGameController>() == null,"Legacy gameplay was loaded.");
                // Compare the charge immediately at each click; normal timed regeneration may occur during transitions.
                view = game.GetComponent<EnglishGameView>();
                if (stage == 1)
                {
                    game.LoadLesson(0);
                    Canvas.ForceUpdateCanvases();
                    Click(view.GetComponentsInChildren<Button>().First(b => b.name == "Pronounce word"));
                    Check(game.GetComponent<AudioSource>().isPlaying,"Speaker did not play in the main flow.");
                    Click(view.GetComponentsInChildren<Button>().First(b => b.name == "Lessons button"));
                    Click(view.GetComponentsInChildren<Button>().Last(b => b.name == "Home button"));
                    stage = 2;
                }
                else
                {
                    Check(game.Course.lessons.Count == 9,"Expanded course did not load.");
                    long energyBeforeCompletion = IPlayerResource.Instance.GetCommonResource(ECommonResource.Energy);
                    int guard = 0;
                    while (!game.Session.IsComplete && guard++ < 100)
                    {
                        var target = view.Flashlight.transform.parent.GetComponentsInChildren<RectTransform>(false).First(r => r.name == game.Session.CurrentWordId);
                        Vector2 position = RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center));
                        var pointer = new PointerEventData(EventSystem.current) { pointerId = 1, position = position };
                        view.Flashlight.OnPointerDown(pointer); view.Flashlight.OnPointerUp(pointer);
                        view.Flashlight.OnPointerDown(pointer); view.Flashlight.OnPointerUp(pointer);
                    }
                    Check(game.Session.IsComplete,"Lesson could not complete inside the main flow.");
                    Check(IPlayerResource.Instance.GetCommonResource(ECommonResource.Energy) == energyBeforeCompletion,"Completing a lesson charged extra Energy.");
                    ScreenCapture.CaptureScreenshot("Temp/EnglishLearning.main-complete.png");
                    stage = 4;
                    nextCheck = EditorApplication.timeSinceStartup + .5;
                }
                deadline = EditorApplication.timeSinceStartup + 40;
            }
            else if (stage == 2)
            {
                if (manager.GameState != EGameState.Home) return;
                Check(SceneManager.GetActiveScene().name == Constant.SCENE_MAIN_UI && UIManager.Instance.Initialized,"Home did not reinitialize after quitting.");
                Check(UnityEngine.Object.FindFirstObjectByType<EnglishLearningGame>() == null,"Old English controller survived return Home.");
                long energyBeforeReplay = IPlayerResource.Instance.GetCommonResource(ECommonResource.Energy);
                Click(UnityEngine.Object.FindFirstObjectByType<HomePlayButton>().button);
                Check(IPlayerResource.Instance.GetCommonResource(ECommonResource.Energy) == energyBeforeReplay - 1,"Second Home Play did not charge exactly 1 Energy.");
                stage = 3; deadline = EditorApplication.timeSinceStartup + 40;
            }
            else if (stage == 4 && EditorApplication.timeSinceStartup >= nextCheck)
            {
                Click(view.GetComponentsInChildren<Button>().Last(b => b.name == "Home button"));
                stage = 5; deadline = EditorApplication.timeSinceStartup + 40;
            }
            else if (stage == 5)
            {
                if (manager.GameState != EGameState.Home) return;
                Check(UIManager.Instance.Initialized && SceneManager.GetActiveScene().name == Constant.SCENE_MAIN_UI,"Result Home action failed.");
                File.WriteAllText(Report,"PASS: Splash -> Loading -> Home Play -> English lessons -> lesson-menu Home -> Home Play again -> complete lesson -> result Home. UI initialized, no legacy controller, correct Energy cost (-1 per entry), pronunciation works, 9-lesson course loaded. Test-touched local resource/timer/lesson saves restored on exit.");
                Debug.Log("English main flow smoke PASS.");
                EditorApplication.isPlaying = false;
            }
        }
        catch (Exception ex) { Fail(ex); }
    }
    private static void Click(Button button)
    {
        Check(button != null && button.isActiveAndEnabled && button.interactable,"Navigation button unavailable.");
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },ExecuteEvents.pointerClickHandler);
    }
    private static void Check(bool condition,string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Fail(Exception ex)
    {
        File.WriteAllText(Report,"FAIL: " + ex);
        Debug.LogException(ex);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        else SessionState.SetBool(Running,false);
    }
}
