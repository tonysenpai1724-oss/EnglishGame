using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneHelper : Singleton<SceneHelper>
{
    protected override void OnAwake()
    {
        base.OnAwake();
        Application.targetFrameRate = 60;
    }
    public void ChangeSceneLoading()
    {
        StartCoroutine(IEChangeSceneLoading());
    }
    public IEnumerator IEChangeSceneLoading()
    {
        Debug.Log("ChangeScene Loading");
        if(UIManager.Instance != null)
            UIManager.Instance.Initialized = false;
        var async = SceneManager.LoadSceneAsync(Constant.SCENE_LOADING);
        while (!async.isDone)
        {
            //totalProgress = async.progress;
            yield return null;
        }
        Debug.Log("ChangeScene Loading Done");
    }
    public IEnumerator IEReturnHome()
    {
        yield return StartCoroutine(LoadingPanel.Instance.IEStartTransiton());
        if (SceneManager.GetActiveScene() != SceneManager.GetSceneByName(Constant.SCENE_LOADING))
            yield return StartCoroutine(IEChangeSceneLoading());
        yield return StartCoroutine(IEChangeSceneHome());
        yield return StartCoroutine(UIManager.Instance.IEHomeInit());
        yield return StartCoroutine(LoadingPanel.Instance.IEEndTransition());
        GameManager.Instance.SetState(EGameState.Home);
        UIManager.Instance.HomeInit();
    }
    IEnumerator IEChangeSceneHome()
    {
        Debug.Log("ChangeScene Home");
        var async = SceneManager.LoadSceneAsync(Constant.SCENE_MAIN_UI);
        while (!async.isDone)
        {
            //totalProgress = async.progress;
            yield return null;
        }
        Debug.Log("ChangeScene Home Done");
    }
    public IEnumerator IEGoGameplay()
    {
        yield return StartCoroutine(LoadingPanel.Instance.IEStartTransiton());
        yield return StartCoroutine(IEChangeSceneLoading());
        yield return StartCoroutine(IEChangeSceneGameplay());
        var englishGame = FindFirstObjectByType<EnglishLearning.EnglishLearningGame>();
        var candySort = FindFirstObjectByType<CandySort.CandySortGameController>();
        if (englishGame != null)
        {
            // English lessons own their UI and board; never wait for legacy map/UI components.
            float deadline = Time.realtimeSinceStartup + 30f;
            while (englishGame != null && !englishGame.IsInitialized && englishGame.InitializationError == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (englishGame == null || !englishGame.IsInitialized)
            {
                Debug.LogError("English gameplay failed to initialize: " +
                    (englishGame != null ? englishGame.InitializationError ?? "Timed out after 30 seconds." : "Controller was destroyed."));
                yield return StartCoroutine(IEReturnHome());
                yield break;
            }
        }
        else if (candySort != null)
            yield return new WaitUntil(() => candySort.IsInitialized);
        else
            yield return StartCoroutine(IELoadMap());
        yield return StartCoroutine(UIManager.Instance.IEGamgeInit(englishGame == null && candySort == null));
        yield return StartCoroutine(LoadingPanel.Instance.IEEndTransition());
        GameManager.Instance.SetState(EGameState.Gameplay);
        if (englishGame != null)
        {
            englishGame.HomeRequested += ReturnFromEnglishLesson;
            englishGame.EnableHomeNavigation();
        }
    }
    private void ReturnFromEnglishLesson()
    {
        if (GameManager.Instance.GameState != EGameState.Gameplay) return;
        GameManager.Instance.GoSceneHome();
    }

    IEnumerator IEChangeSceneGameplay()
    {
        Debug.Log("ChangeScene Gameplay");
        var async = SceneManager.LoadSceneAsync(Constant.SCENE_GAME_PLAY);
        while (!async.isDone)
        {
            //totalProgress = async.progress;
            yield return null;
        }
        Debug.Log("ChangeScene Gameplay Done");
    }

    public IEnumerator IELoadMap()
    {
        yield return new WaitUntil(() => GameplayManager.Instance);
        yield return StartCoroutine(GameplayManager.Instance.IEInit());
    }
}
