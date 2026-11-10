using System;
using System.IO;
using System.Linq;
using EnglishLearning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit editor tools. Never regenerate or overwrite authored lesson content.</summary>
public static class EnglishLearningTools
{
    public const string ScenePath = "Assets/Scenes/EnglishLearning.unity";
    public const string CoursePath = "Assets/EnglishLearning/Resources/EnglishLearning/DemoCourse.asset";

    [MenuItem("Tools/English Learning/Open Game Scene")]
    public static void OpenGame()
    {
        if (EditorApplication.isPlaying) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/English Learning/Open Main Flow (Splash)")]
    public static void OpenMainFlow()
    {
        if (EditorApplication.isPlaying) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity");
    }

    [MenuItem("Tools/English Learning/Select Demo Course")]
    public static void SelectCourse()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<EnglishCourse>(CoursePath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem("Tools/English Learning/Validate Demo Course and Model")]
    public static void ValidateDemo()
    {
        var course = AssetDatabase.LoadAssetAtPath<EnglishCourse>(CoursePath);
        Require(course != null,"Demo course could not be loaded.");
        var errors = course.ValidateContent();
        Require(errors.Count == 0,string.Join("\n",errors));
        foreach (var word in course.vocabulary)
            Require(word.pronunciation != null && word.pronunciation.length > 0,"Missing pronunciation: " + word.id);
        Require(Resources.Load<Shader>("EnglishLearning/Flashlight") != null,"Missing flashlight shader.");
        LessonSessionTests.RunAll();
        Debug.Log("English Learning: course, ten pronunciation clips, shader reference, and model tests PASS.");
    }

    private static void Require(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // One-shot automation for an already-open editor (it cannot also open in batch mode).
    // Write Temp/EnglishLearning.validate.request, then focus/refresh Unity.
    [InitializeOnLoadMethod]
    private static void RegisterValidationRequest()
    {
        EditorApplication.update -= CheckValidationRequest;
        EditorApplication.update += CheckValidationRequest;
    }
    private static double nextCheck;
    private static void CheckValidationRequest()
    {
        if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        const string request = "Temp/EnglishLearning.validate.request";
        if (!File.Exists(request)) return;
        File.Delete(request);
        try
        {
            ValidateDemo();
            File.WriteAllText("Temp/EnglishLearning.validation.txt","PASS: course, audio, shader reference, model tests.");
        }
        catch (Exception ex)
        {
            File.WriteAllText("Temp/EnglishLearning.validation.txt","FAIL: " + ex);
            Debug.LogException(ex);
        }
    }
}
