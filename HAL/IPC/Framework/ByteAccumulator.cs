using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;

namespace Redbox.HAL.IPC.Framework
{
    internal sealed class ByteAccumulator : IIPCResponse, IDisposable
    {
        internal readonly List<byte> Accumulator = new List<byte>();
        private bool Disposed;

        public void Dispose()
        {
            if (this.Disposed)
                return;
            this.Disposed = true;
            this.Accumulator.Clear();
            GC.SuppressFinalize((object)this);
        }

        public bool Accumulate(byte[] rawResponse)
        {
            return this.Accumulate(rawResponse, 0, rawResponse.Length);
        }

        public bool Accumulate(byte[] bytes, int start, int length)
        {
            for (int index = start; index < length; ++index)
                this.Accumulator.Add(bytes[index]);
            return true;
        }

        public bool OnReadComplete() => this.IsComplete = true;

        public void Clear()
        {
            this.Accumulator.Clear();
            this.IsComplete = false;
        }

        public bool IsComplete { get; private set; }
    }
}
