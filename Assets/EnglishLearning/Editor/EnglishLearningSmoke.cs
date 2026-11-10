using System;
using System.IO;
using System.Linq;
using EnglishLearning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One-shot Play Mode acceptance test: real UI, pointer handlers, audio and screenshots.</summary>
[InitializeOnLoad]
public static class EnglishLearningSmoke
{
    private const string Running = "EnglishLearning.Smoke.Running";
    private const string Request = "Temp/EnglishLearning.smoke.request";
    private const string Report = "Temp/EnglishLearning.smoke.txt";
    private static double deadline, nextCheck;
    private static int stage, assertions;
    private static EnglishLearningGame game;
    private static EnglishGameView view;
    private static bool failed;

    static EnglishLearningSmoke()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode) { stage = 0; assertions = 0; deadline = 0; failed = false; }
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Running,false)) return;
            string key = SessionState.GetString("EnglishLearning.Smoke.ProgressKey","");
            if (SessionState.GetBool("EnglishLearning.Smoke.HadProgress",false))
                PlayerPrefs.SetInt(key,SessionState.GetInt("EnglishLearning.Smoke.Progress",0));
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            SessionState.SetBool(Running,false);
        };
    }

    [MenuItem("Tools/English Learning/Run Gameplay Smoke Test")]
    public static void RequestSmoke()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before starting the smoke test.");
        File.WriteAllText(Request,"run");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request)) return;
            File.Delete(Request);
            try
            {
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    Check(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty,"Save open scenes before running the smoke test.");
                EnglishLearningTools.ValidateDemo();
                var course = AssetDatabase.LoadAssetAtPath<EnglishCourse>(EnglishLearningTools.CoursePath);
                SessionState.SetString("EnglishLearning.Smoke.ProgressKey",course.progressKey);
                SessionState.SetBool("EnglishLearning.Smoke.HadProgress",PlayerPrefs.HasKey(course.progressKey));
                SessionState.SetInt("EnglishLearning.Smoke.Progress",PlayerPrefs.GetInt(course.progressKey));
                EditorSceneManager.OpenScene(EnglishLearningTools.ScenePath);
                SessionState.SetBool(Running,true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex) { File.WriteAllText(Report,"FAIL before Play Mode: " + ex); Debug.LogException(ex); }
            return;
        }
        if (!SessionState.GetBool(Running,false) || failed) return;
        try
        {
            if (stage == 0)
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 40;
                game = UnityEngine.Object.FindFirstObjectByType<EnglishLearningGame>();
                if (game == null || !game.IsInitialized)
                {
                    Check(EditorApplication.timeSinceStartup < deadline,"Game failed to initialize within 40 seconds.");
                    return;
                }
                assertions = 0;
                view = game.GetComponent<EnglishGameView>();
                game.LoadLesson(0);
                stage = 1; deadline = EditorApplication.timeSinceStartup + .5;
            }
            else if (stage == 1 && EditorApplication.timeSinceStartup >= deadline)
            {
                Check(view.Flashlight.material.shader.isSupported,"Flashlight shader is not supported.");
                Check(!UnityEditor.ShaderUtil.ShaderHasError(view.Flashlight.material.shader),"Flashlight shader has compile errors.");
                Canvas.ForceUpdateCanvases();
                var mesh = view.Flashlight.canvasRenderer.GetMesh();
                Check(mesh != null && mesh.vertexCount >= 4,"Flashlight overlay generated no mesh.");
                File.WriteAllText("Temp/EnglishLearning.render-state.txt", "overlay active=" + view.Flashlight.gameObject.activeInHierarchy +
                    " cull=" + view.Flashlight.canvasRenderer.cull + " color=" + view.Flashlight.color +
                    " material=" + view.Flashlight.material.name + " rect=" + view.Flashlight.rectTransform.rect +
                    " radius=" + view.Flashlight.material.GetFloat("_Radius") + " center=" + view.Flashlight.material.GetVector("_Center"));
                var rect = view.Flashlight.rectTransform;
                Check(rect.rect.width > 100 && rect.rect.height > 100,"Play field has invalid dimensions.");
                string current = game.Session.CurrentWordId;
                Vector2 target = ObjectScreenPosition(current);
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(Pointer(target),hits);
                Check(hits.Count > 0 && hits[0].gameObject == view.Flashlight.gameObject,"The flashlight does not receive board raycasts.");
                // A point at the top left corner is always outside the initial circle.
                Vector2 darkCorner = RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.min + new Vector2(4,4)));
                Tap(darkCorner);
                Check(game.Session.CompletedCount == 0,"Tapping empty darkness advanced the lesson.");
                view.Flashlight.ResetLight();
                // Start a drag at center, finish at a target: dragging must not select.
                var drag = Pointer(RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
                view.Flashlight.OnPointerDown(drag);
                drag.position = target; view.Flashlight.OnDrag(drag); view.Flashlight.OnPointerUp(drag);
                Check(game.Session.CompletedCount == 0,"Dragging selected an object.");
                // Explore an incorrect object and select it; the displayed word must not change.
                string wrong = game.Course.lessons[0].objects.First(p => p.wordId != current).wordId;
                Tap(ObjectScreenPosition(wrong)); Tap(ObjectScreenPosition(wrong));
                Check(game.Session.CurrentWordId == current && game.Session.CompletedCount == 0,"Wrong answer changed target.");
                // Reset, first touch reveals; second touch selects.
                view.Flashlight.ResetLight();
                Tap(target);
                Check(game.Session.CompletedCount == 0,"First tap selected a previously hidden object.");
                ScreenCapture.CaptureScreenshot("Temp/EnglishLearning.gameplay.png");
                stage = 2; deadline = EditorApplication.timeSinceStartup + .5;
            }
            else if (stage == 2 && EditorApplication.timeSinceStartup >= deadline)
            {
                Tap(ObjectScreenPosition(game.Session.CurrentWordId));
                Check(game.Session.CompletedCount == 1,"Second tap on a lit correct object did not advance.");
                Click("Pronounce word");
                var audio = game.GetComponent<AudioSource>();
                Check(audio != null && audio.isPlaying && audio.clip == game.Course.FindWord(game.Session.CurrentWordId).pronunciation,"Speaker did not play the current word clip.");
                Click("Restart button");
                Check(game.Session.CompletedCount == 0,"Restart did not reset the lesson.");
                var mouse = Pointer(ObjectScreenPosition(game.Session.CurrentWordId));
                mouse.pointerId = -1;
                view.Flashlight.OnPointerMove(mouse);
                mouse.button = PointerEventData.InputButton.Right;
                view.Flashlight.OnPointerDown(mouse); view.Flashlight.OnPointerUp(mouse);
                Check(game.Session.CompletedCount == 0,"Right click selected an object.");
                mouse.button = PointerEventData.InputButton.Left;
                view.Flashlight.OnPointerDown(mouse);
                Tap(mouse.position); // A second pointer cannot steal the active mouse/gesture.
                Check(game.Session.CompletedCount == 0,"A second pointer stole an active gesture.");
                view.Flashlight.OnPointerUp(mouse);
                Check(game.Session.CompletedCount == 1,"Mouse hover followed by a click did not select.");
                Click("Restart button");
                Check(game.Session.CompletedCount == 0,"Restart after mouse input did not reset.");
                Click("Lessons button");
                Check(!view.Flashlight.InputEnabled,"Lesson menu did not block gameplay input.");
                Click("2. " + game.Course.lessons[1].title + " button");
                Check(game.LessonIndex == 1 && game.Session.CompletedCount == 0,"Lesson picker did not open the selected lesson.");
                for (int lesson = 0; lesson < game.Course.lessons.Count; lesson++)
                {
                    game.LoadLesson(lesson);
                    int guard = 0;
                    while (!game.Session.IsComplete && guard++ < 100)
                    {
                        Vector2 position = ObjectScreenPosition(game.Session.CurrentWordId);
                        Tap(position); Tap(position);
                    }
                    Check(game.Session.IsComplete,"Could not complete lesson " + lesson);
                    Check(!view.Flashlight.gameObject.activeSelf,"Completed lesson remained covered by darkness.");
                    if (lesson + 1 < game.Course.lessons.Count)
                    {
                        Click("Next lesson button");
                        Check(game.LessonIndex == lesson+1,"Next lesson button failed.");
                    }
                }
                ScreenCapture.CaptureScreenshot("Temp/EnglishLearning.complete.png");
                stage = 3; deadline = EditorApplication.timeSinceStartup + .5;
            }
            else if (stage == 3 && EditorApplication.timeSinceStartup >= deadline)
            {
                Click("Choose a lesson button");
                Check(!view.Flashlight.InputEnabled,"Final result did not open lesson picker.");
                Click("1. " + game.Course.lessons[0].title + " button");
                Check(game.LessonIndex == 0 && !game.Session.IsComplete,"Course could not be replayed.");
                File.WriteAllText(Report,"PASS: " + assertions + " runtime assertions; all " + game.Course.lessons.Count + " lessons, hidden-first-tap, reveal/drag/select, wrong/correct answers, speaker audio, restart, lesson picker, next/replay.\nScreenshots: Temp/EnglishLearning.gameplay.png and Temp/EnglishLearning.complete.png");
                Debug.Log("English Learning gameplay smoke PASS (" + assertions + " assertions).");
                EditorApplication.isPlaying = false;
            }
        }
        catch (Exception ex)
        {
            failed = true; File.WriteAllText(Report,"FAIL: " + ex); Debug.LogException(ex);
            EditorApplication.isPlaying = false;
        }
    }

    private static PointerEventData Pointer(Vector2 position) => new PointerEventData(EventSystem.current)
        { pointerId = 1, position = position, button = PointerEventData.InputButton.Left };
    private static void Tap(Vector2 position)
    {
        var pointer = Pointer(position);
        view.Flashlight.OnPointerDown(pointer); view.Flashlight.OnPointerUp(pointer);
    }
    private static Vector2 ObjectScreenPosition(string id)
    {
        var item = view.Flashlight.transform.parent.GetComponentsInChildren<RectTransform>(false)
            .FirstOrDefault(rect => rect.name == id);
        Check(item != null,"Missing object view " + id);
        return RectTransformUtility.WorldToScreenPoint(null,item.TransformPoint(item.rect.center));
    }
    private static void Click(string name)
    {
        var button = view.GetComponentsInChildren<Button>(false).FirstOrDefault(b => b.name == name);
        Check(button != null && button.interactable,"Missing or disabled button " + name);
        ExecuteEvents.Execute(button.gameObject,Pointer(Vector2.zero),ExecuteEvents.pointerClickHandler);
    }
    private static void Check(bool condition,string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
