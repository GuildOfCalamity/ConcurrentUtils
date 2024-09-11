using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ConcurrentUtils.Test
{
    [TestFixture]
    public class QueueTests : BaseUnitTest
    {
        [Test]
        public async Task Enqueue_Should_Complete_Task_When_Job_Is_Completed_According_To_Limit(int limit)
        {
            var tcsArray = GetTaskCompletionSources<bool>(50);
            Console.WriteLine(" ⇒ Started");
            var started = 0;
            Func<long, Task> method = index =>
            {
                Interlocked.Increment(ref started);
                return tcsArray[index % tcsArray.Length].Task;
            };
            var jobQueue = ConcurrentUtils.CreateQueue(limit, method);
            var tasks = tcsArray.Select((tcs, i) => jobQueue.Enqueue(i)).ToArray();
            var vrStarted = Volatile.Read(ref started);
            Assert.That(limit, Is.EqualTo(vrStarted));
            Assert.That(jobQueue.Count, Is.EqualTo(tcsArray.Length));
            for (var i = 0; i < tcsArray.Length; i++)
            {
                tcsArray[i].SetResult(true);
                await Task.Delay(10);
                Assert.That(TaskStatus.RanToCompletion, Is.EqualTo(tasks[i].Status));
                var expectedStarted = limit + 1 + i;
                if (expectedStarted > tcsArray.Length)
                {
                    expectedStarted = tcsArray.Length;
                }
                vrStarted = Volatile.Read(ref started);
                Assert.That(expectedStarted, Is.EqualTo(vrStarted));
                Assert.That(tcsArray.Length - i - 1, Is.EqualTo(jobQueue.Count));
            }
            Console.WriteLine(" ⇒ Finished");
        }

        [Test]
        public async Task Enqueue_Should_Fault_Task_When_Job_Failed()
        {
            var tcsArray = GetTaskCompletionSources<bool>(10);
            Console.WriteLine($" ⇒ Started");
            Func<long, Task> method = index => tcsArray[index % tcsArray.Length].Task;
            var jobQueue = ConcurrentUtils.CreateQueue(4, method);
            var tasks = tcsArray.Select((tcs, i) => jobQueue.Enqueue(i)).ToArray();
            Assert.That(jobQueue.Count, Is.EqualTo(tcsArray.Length));
            for (var i = 0; i < tcsArray.Length; i++)
            {
                if (i != tcsArray.Length/2)
                {
                    tcsArray[i].SetResult(true);
                }
                else // inject an exception
                {
                    tcsArray[i].SetException(new Exception("Test exception from Enqueue_Should_Fault_Task_When_Job_Failed()"));
                }
            }
            await Task.Delay(10);
            Assert.That(tcsArray.Length - 1, Is.EqualTo(tasks.Count(t => t.Status == TaskStatus.RanToCompletion)));
            Assert.That(1, Is.EqualTo(tasks.Count(t => t.Status == TaskStatus.Faulted)));
            Console.WriteLine(" ⇒ Finished");
        }

        [Test]
        public async Task Enqueue_Should_Fault_Task_When_Func_Throws()
        {
            var tcsArray = GetTaskCompletionSources<bool>(10);
            Console.WriteLine($" ⇒ Started");
            Func<int, Task> method = index =>
            {
                if (index == tcsArray.Length/2)
                {
                    throw new Exception("Test exception from Enqueue_Should_Fault_Task_When_Func_Throws()");
                }
                return tcsArray[index%tcsArray.Length].Task;
            };
            var jobQueue = ConcurrentUtils.CreateQueue(4, method);
            var tasks = tcsArray.Select((tcs, i) => jobQueue.Enqueue(i)).ToArray();
            Assert.That(tcsArray.Length, Is.EqualTo(jobQueue.Count));
            for (var i = 0; i < tcsArray.Length; i++)
            {
                if (i != tcsArray.Length / 2)
                {
                    tcsArray[i].SetResult(true);
                }
            }
            await Task.Delay(100);
            Assert.That(tcsArray.Length - 1, Is.EqualTo(tasks.Count(t => t.Status == TaskStatus.RanToCompletion)));
            Assert.That(1, Is.EqualTo(tasks.Count(t => t.Status == TaskStatus.Faulted)));
            Console.WriteLine(" ⇒ Finished");
        }
    }
}
