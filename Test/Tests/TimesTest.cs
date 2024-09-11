using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ConcurrentUtils.Test
{
    [TestFixture]
    public class TimesTests : BaseUnitTest
    {
        [Test]
        public async Task Should_Complete_Task_When_All_Tasks_Are_Completed()
        {
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            Console.WriteLine(" ⇒ Started");
            var indexes = new long?[1000];
            Func<long, Task> method = index =>
            {
                indexes[index] = index;
                if (index == 5)
                {
                    return tcs2.Task;
                }
                return tcs1.Task;
            };
            var timesTask = ConcurrentUtils.Times(1000, 10, method);
            Assert.That(TaskStatus.WaitingForActivation, Is.EqualTo(timesTask.Status));
            tcs1.SetResult(true);
            Assert.That(TaskStatus.WaitingForActivation, Is.EqualTo(timesTask.Status));
            await Task.Delay(40);
            Assert.That(TaskStatus.WaitingForActivation, Is.EqualTo(timesTask.Status));
            // Assert that all the indexes where passed starting from 0
            for (long i = 0; i < indexes.Length; i++)
            {
                Assert.That(i, Is.EqualTo(indexes[i]));
            }
            tcs2.SetResult(true);
            Assert.That(TaskStatus.RanToCompletion, Is.EqualTo(timesTask.Status));
            Console.WriteLine(" ⇒ Finished");
        }

        [Test]
        public void Should_Fault_Task_When_Any_Of_The_Tasks_Are_Faulted()
        {
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            Console.WriteLine(" ⇒ Started");
            Func<long, Task> method = index =>
            {
                if (index == 15)
                {
                    return tcs2.Task;
                }
                return tcs1.Task;
            };
            var timesTask = ConcurrentUtils.Times(1000, 10, method);
            Assert.That(TaskStatus.WaitingForActivation, Is.EqualTo(timesTask.Status));
            tcs1.SetResult(true);
            Assert.That(TaskStatus.WaitingForActivation, Is.EqualTo(timesTask.Status));
            var ex = new Exception("Test exception");
            tcs2.SetException(ex);
            Assert.That(TaskStatus.Faulted, Is.EqualTo(timesTask.Status));
            Assert.That(ex, Is.EqualTo(timesTask.Exception.InnerException));
            Console.WriteLine(" ⇒ Finished");
        }

        [Test]
        public void Should_Start_At_Most_Limit_Operations_In_Parallel()
        {
            var tcsArray = GetTaskCompletionSources<bool>(100);
            Console.WriteLine(" ⇒ Started");
            var counter = 0;
            Func<long, Task> method = index =>
            {
                Interlocked.Increment(ref counter);
                return tcsArray[index%tcsArray.Length].Task;
            };
            var timesTask = ConcurrentUtils.Times(1000, 10, method);
            Assert.That(TaskStatus.WaitingForActivation, Is.EqualTo(timesTask.Status));
            Assert.That(Volatile.Read(ref counter), Is.EqualTo(10));
            for (var i = 0; i < 10; i++)
            {
                tcsArray[i].SetResult(true);
                Assert.That(Volatile.Read(ref counter), Is.EqualTo(11 + i));
            }
            Assert.That(TaskStatus.WaitingForActivation, Is.EqualTo(timesTask.Status));
            for (var i = 10; i < tcsArray.Length; i++)
            {
                tcsArray[i].SetResult(true);
            }
            Assert.That(TaskStatus.RanToCompletion, Is.EqualTo(timesTask.Status));
            Assert.That(Volatile.Read(ref counter), Is.EqualTo(1000));
            Console.WriteLine(" ⇒ Finished");
        }
    }
}
