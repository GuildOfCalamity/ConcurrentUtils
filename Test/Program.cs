using ConcurrentUtils;
using ConcurrentUtils.Test;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            ListReferencedAssemblies();

            #region [Manual Tests]
            var tt = new TimesTests();
            var qt = new QueueTests();
            var mt = new MapTests();
            
            Task.Run(async () =>
            {
                using (StopClock sc = new StopClock(color: ConsoleColor.Yellow))
                {
                    Console.WriteLine($"\r\n• Running {nameof(MapTests)}…");
                    await mt.Should_Map_Async();
                }
            
                using (StopClock sc = new StopClock(color: ConsoleColor.Green))
                {
                    Console.WriteLine($"\r\n• Running {nameof(TimesTests)}…");
                    tt.Should_Start_At_Most_Limit_Operations_In_Parallel();
                    tt.Should_Fault_Task_When_Any_Of_The_Tasks_Are_Faulted();
                    await tt.Should_Complete_Task_When_All_Tasks_Are_Completed();
                }
            
                using (StopClock sc = new StopClock(color: ConsoleColor.Cyan))
                {
                    Console.WriteLine($"\r\n• Running {nameof(QueueTests)}…");
                    await qt.Enqueue_Should_Fault_Task_When_Job_Failed();
                    await qt.Enqueue_Should_Complete_Task_When_Job_Is_Completed_According_To_Limit(10);
                    await qt.Enqueue_Should_Fault_Task_When_Func_Throws();
                    await qt.Enqueue_Should_Fault_Task_When_Job_Failed();
                }

                //PracticalLoggerTest();
            });
            #endregion

            Console.WriteLine($"\r\n• PRESS ANY KEY TO EXIT •\r\n");
            _ = Console.ReadKey(true);
        }

        static void ListReferencedAssemblies()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName main = assembly.GetName();
            Console.WriteLine($"Main Assembly: {main.Name}, Version: {main.Version}");
            foreach (var sas in assembly.GetReferencedAssemblies().OrderBy(o => o.Name))
            {
                Console.WriteLine($" Sub Assembly: {sas.Name}, Version: {sas.Version}");
            }
        }

        /// <summary>
        /// A test for deferring writes to a log file.
        /// </summary>
        static void PracticalLoggerTest()
        {
            Func<string, Task> logMethod = message =>
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), $"{Assembly.GetExecutingAssembly().GetName().Name}.log"), append: true, Encoding.UTF8))
                    {
                        return writer.WriteLineAsync($"{message}");
                    }
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            };

            Task.Run(async () =>
            {
                using (StopClock sc = new StopClock(color: ConsoleColor.Yellow))
                {
                    IJobQueue<string> jobQueue = ConcurrentUtils.ConcurrentUtils.CreateQueue(2, logMethod);
                    Task[] tasks = new Task[10];
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        int num = i;
                        tasks[i] = jobQueue.Enqueue($"{DateTime.Now.ToString("hh:mm:ss.fff tt")} log line {num}");
                    }
                    await Task.WhenAll(tasks);
                }
            }).ContinueWith(t =>
            {
                Console.WriteLine($"• LoggerTaskStatus: {t.Status}");
            });
        }
    }
}
