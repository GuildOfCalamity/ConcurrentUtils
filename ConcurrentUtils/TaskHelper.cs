using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrentUtils
{
    /// <summary>
    /// Contains Task extensions.
    /// </summary>
    internal static class TaskHelper
    {
        internal static async void FireAndForget(this Task task, bool continueOnCapturedContext = false)
        {
            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] FireAndForget: {ex.Message}");
            }
        }
    }
}
