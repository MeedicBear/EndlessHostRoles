using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace EHR;

internal static class LateTask
{
    private static int CurrentEpoch;

    /// <summary>
    ///     Cancels all pending cancellable LateTasks (e.g. on disconnect or game reset).
    /// </summary>
    public static void CancelAll()
    {
        Interlocked.Increment(ref CurrentEpoch);
    }

    /// <summary>
    ///     Creates a task that will be automatically completed after the specified amount of time
    /// </summary>
    /// <param name="action">The delayed task</param>
    /// <param name="time">The time to wait until the task is run</param>
    /// <param name="name">The name of the task</param>
    /// <param name="log">Whether to send log of the creation and completion of the Late Task</param>
    /// <param name="cancelOnReset">Whether to cancel this task if CancelAll() is called before it runs</param>
    public static void New(Action action, float time, string name = "No Name Task", bool log = true, [CallerFilePath] string path = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, bool cancelOnReset = true)
    {
        if (action == null || Main.Instance == null) return;

        int capturedEpoch = Volatile.Read(ref CurrentEpoch);
        if (log && name is not "" and not "No Name Task") Logger.Info($"\"{name}\" is created (completes in {time:N2})", "LateTask");
        Main.Instance.StartCoroutine(CoLateTask());
        return;

        IEnumerator CoLateTask()
        {
            yield return new WaitForSecondsRealtime(time);

            if (cancelOnReset && capturedEpoch != Volatile.Read(ref CurrentEpoch))
                yield break;

            try
            {
                action();
            
                if (name is not "" and not "No Name Task" && log)
                    Logger.Info($"\"{name}\" is finished", "LateTask");
            }
            catch (Exception ex) { Logger.Error($"{ex.GetType()}: {ex.Message}\n  in \"{name}\"\n  (created at {Path.GetFileName(path)}, by member {member}, at line {line})\n  {ex.StackTrace}".Replace("\r\n", "\n"), "LateTask.Error", false, multiLine: true); }
        }
    }
}