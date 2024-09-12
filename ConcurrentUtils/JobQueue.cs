using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentUtils
{
    /// <summary>
    /// Represents a thread-safe FIFO collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IJobQueue<T> : IDisposable
    {
        /// <summary>
        /// Event that is raised once the job queue is empty.
        /// </summary>
        event Action<DateTime>? Drained;

        /// <summary>
        /// Event that is raised when the task was not able to complete.
        /// </summary>
        event Action<Exception>? UnhandledException;

        /// <summary>
        /// Gets the amount of queued items.
        /// </summary>
        long Count { get; }

        /// <summary>
        /// Adds an object to the end of the <see cref="IJobQueue{T}"/> and returns a <see cref="Task"/> that
        /// will be completed when the job will be completed.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <exception cref="ObjectDisposedException" />
        Task Enqueue(T item);

        /// <summary>
        /// Adds an object to the end of the <see cref="IJobQueue{T}"/> and does not propagate exceptions.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <exception cref="ObjectDisposedException" />
        Task EnqueueIgnoreExceptions(T item);

        /// <summary>
        /// Adds an object to the end of the <see cref="IJobQueue{T}"/>.
        /// Any <see cref="Exception"/>s thrown will not be propagated.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <exception cref="ObjectDisposedException" />
        void TryEnqueue(T item);
    }

    /// <summary>
    /// Represents a thread-safe FIFO collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class JobQueue<T> : IJobQueue<T>
    {
        private readonly Func<T, Task> _method;
        private readonly SemaphoreSlim _semaphore;
        private volatile bool _isDisposed;
        private long _queuedCount;

        public event Action<DateTime>? Drained;

        public event Action<Exception>? UnhandledException;

        public long Count 
        { 
            get => Volatile.Read(ref _queuedCount); 
        }

        internal JobQueue(int limit, Func<T, Task> method)
        {
            _method = method;
            _semaphore = new SemaphoreSlim(limit);
        }

        public Task Enqueue(T item)
        {
            return Enqueue(item, true);
        }

        public Task EnqueueIgnoreExceptions(T item)
        {
            return Enqueue(item, false);
        }

        private Task Enqueue(T item, bool throwExceptions)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("JobQueue");
            }
            return Run(item, throwExceptions);
        }

        public void TryEnqueue(T item)
        {
            Enqueue(item, false).FireAndForget();
        }

        /// <summary>
        /// Awaits an available slot and starts the async operation.
        /// </summary>
        private async Task Run(T item, bool throwExceptions)
        {
            Interlocked.Increment(ref _queuedCount);
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                var task = _method(item);
                await task.ConfigureAwait(false);
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // We don't mind if the semaphore has been disposed at this moment.
                }
            }
            catch (Exception ex)
            {
                UnhandledException?.Invoke(ex);
                if (throwExceptions)
                    throw;
            }
            finally
            {
                var empty = Interlocked.Decrement(ref _queuedCount) == 0;
                if (empty)
                    Drained?.Invoke(DateTime.Now);
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            _semaphore.Dispose();
        }

    }
}
