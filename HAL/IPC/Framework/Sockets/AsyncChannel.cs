using Redbox.HAL.Component.Model;
using System;
using System.IO;
using System.Threading;

namespace Redbox.HAL.IPC.Framework.Sockets
{
    internal sealed class AsyncChannel : IDisposable, IIPCChannel
    {
        private bool Disposed;
        private readonly string Identifier;
        private readonly bool LogDetails;
        private readonly Stream Stream;
        private readonly byte[] ReadBuffer;
        private readonly AutoResetEvent ReadEvent = new AutoResetEvent(false);

        public void Dispose()
        {
            if (this.Disposed)
                return;
            if (this.LogDetails)
                LogHelper.Instance.Log("[AsyncChannel-{0}] Channel disposed.", (object)this.Identifier);
            this.Disposed = true;
            this.Disconnect();
        }

        public byte[] Read() => this.Read(30000);

        public byte[] Read(int timeout)
        {
            ByteAccumulator response = new ByteAccumulator();
            this.Read((IIPCResponse)response, timeout);
            return !response.IsComplete ? new byte[0] : response.Accumulator.ToArray();
        }

        public void Read(IIPCResponse response) => this.Read(response, 30000);

        public void Read(IIPCResponse response, int timeout)
        {
            this.OnRead(response);
            if (this.ReadEvent.WaitOne(timeout))
                return;
            response.Clear();
        }

        public bool Write(byte[] bytes)
        {
            if (!this.Stream.CanWrite)
                return false;
            try
            {
                this.Stream.Write(bytes, 0, bytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("[AsyncChannel] There was an unhandled exception during write.", ex);
                return false;
            }
        }

        public bool Connect() => this.IsConnected = true;

        public bool Disconnect()
        {
            if (this.IsConnected)
            {
                this.Stream.Dispose();
                this.ReadEvent.Close();
                this.IsConnected = false;
            }
            return true;
        }

        public bool IsConnected { get; private set; }

        internal AsyncChannel(Stream stream, int bufferSize, string id)
          : this(stream, bufferSize, id, false)
        {
        }

        internal AsyncChannel(Stream stream, int bufferSize, string id, bool logDetails)
        {
            this.Stream = stream;
            this.ReadBuffer = new byte[bufferSize];
            this.LogDetails = logDetails;
            this.Identifier = id;
        }

        private void OnRead(IIPCResponse response)
        {
            try
            {
                this.Stream.BeginRead(this.ReadBuffer, 0, this.ReadBuffer.Length, new AsyncCallback(this.EndReadCallback), (object)response);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("[AsyncChannel] Read caught an exception", ex);
                this.SafeSet();
            }
        }

        private void EndReadCallback(IAsyncResult result)
        {
            try
            {
                int length = this.Stream.EndRead(result);
                IIPCResponse asyncState = result.AsyncState as IIPCResponse;
                if (length == 0)
                {
                    this.SafeSet();
                }
                else
                {
                    Interlocked.Add(ref Statistics.Instance.NumberOfBytesReceived, (long)length);
                    if (!asyncState.Accumulate(this.ReadBuffer, 0, length))
                        this.OnRead(asyncState);
                    else
                        this.SafeSet();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(string.Format("[AsyncChannel -{0}] EndReadCallback has caught an unhandled exception.", (object)this.Identifier), ex);
                this.SafeSet();
            }
        }

        private bool SafeSet()
        {
            try
            {
                this.ReadEvent.Set();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("[AsyncChannel] Exception caught during event set.", ex);
                return false;
            }
        }
    }
}
