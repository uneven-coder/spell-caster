using System;
using System.Threading.Tasks;

public static class DelayedActionRunner
{
    public static async void RunWithDelay(Action action, int delayMilliseconds, Action onComplete = null)
    {   // Delays execution of an action by the specified milliseconds
        await Task.Delay(delayMilliseconds);
        action?.Invoke();
        onComplete?.Invoke();  // Execute callback after action completes
    }
}
