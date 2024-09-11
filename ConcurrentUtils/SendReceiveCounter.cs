using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentUtils
{
    /// <summary>
    /// Represents a dual counter
    /// </summary>
    internal class SendReceiveCounter
    {
        long _receiveCounter;
        long _sendCounter;

        public long IncrementSent()
        {
            return Interlocked.Increment(ref _sendCounter);
        }

        public long IncrementReceived()
        {
            return Interlocked.Increment(ref _receiveCounter);
        }
    }
}
