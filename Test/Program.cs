using ConcurrentUtils.Test;
using System.Reflection;
using System.Text;

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
    }
}
