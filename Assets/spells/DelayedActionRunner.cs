// filepath: c:\Users\callu\spell caster\Assets\spells\DelayedActionRunner.cs
using System;
using System.Collections;
using UnityEngine;

public static class DelayedActionManager
{   // Static helper class for running delayed actions in Unity's lifecycle

    private static MonoBehaviourRunner _runner;
    
    // Internal MonoBehaviour to run coroutines
    private class MonoBehaviourRunner : MonoBehaviour
    {   // Internal class to handle the actual coroutines
        public void Awake()
        {   // Set up the runner to persist between scene loads
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private static MonoBehaviourRunner Runner
    {   // Lazy-initialized runner instance
        get
        {
            if (_runner == null)
            {
                GameObject go = new GameObject("DelayedActionManager");
                _runner = go.AddComponent<MonoBehaviourRunner>();
            }
            return _runner;
        }
    }
    
    // Run an action after a specified delay in milliseconds
    public static void RunWithDelay(Action action, int delayMilliseconds)
    {   // Schedule a delayed action using coroutines
        if (action == null) return;
        
        Runner.StartCoroutine(DelayedActionCoroutine(action, delayMilliseconds));
    }
    
    // Coroutine for executing delayed actions
    private static IEnumerator DelayedActionCoroutine(Action action, int delayMilliseconds)
    {   // Wait for the specified time and then execute the action
        if (delayMilliseconds > 0)
            yield return new WaitForSeconds(delayMilliseconds / 1000f);
            
        try
        {
            action?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in delayed action: {e.Message}");
        }
    }
    
    // Schedule a repeating action with a specified interval
    public static Coroutine RunRepeating(Action action, float intervalSeconds, float startDelaySeconds = 0)
    {   // Schedule a repeating action using coroutines
        if (action == null) return null;
        
        return Runner.StartCoroutine(RepeatingActionCoroutine(action, intervalSeconds, startDelaySeconds));
    }
    
    // Coroutine for executing repeating actions
    private static IEnumerator RepeatingActionCoroutine(Action action, float intervalSeconds, float startDelaySeconds)
    {   // Wait for initial delay then repeatedly execute the action
        if (startDelaySeconds > 0)
            yield return new WaitForSeconds(startDelaySeconds);
            
        while (true)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in repeating action: {e.Message}");
            }
            
            yield return new WaitForSeconds(intervalSeconds);
        }
    }
    
    // Cancel a scheduled repeating action
    public static void CancelRepeatingAction(Coroutine coroutine)
    {   // Stop a coroutine if it's still running
        if (coroutine != null && _runner != null)
            _runner.StopCoroutine(coroutine);
    }
}