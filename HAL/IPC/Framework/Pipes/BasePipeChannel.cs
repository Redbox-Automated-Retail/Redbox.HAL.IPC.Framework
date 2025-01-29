using Microsoft.Win32.SafeHandles;
using Redbox.HAL.Component.Model;
using System;
using System.Runtime.InteropServices;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    public abstract class BasePipeChannel : IIPCChannel, IDisposable
    {
        protected readonly string ID;
        protected SafeFileHandle PipeHandle;
        private bool Disposed;
        private readonly int MessageHeaderSize = Marshal.SizeOf(typeof(int));
        private readonly bool FlushOnWrite;

        public void Dispose()
        {
            this.DisposeChannel(true);
            GC.SuppressFinalize((object)this);
        }

        public bool Disconnect()
        {
            if (!this.IsConnected)
                return false;
            this.IsConnected = false;
            return this.OnDisconnect();
        }

        public bool Connect() => this.OnConnect();

        public bool Write(byte[] message)
        {
            if (this.PipeHandle.IsInvalid)
                return false;
            int lpNumberOfBytesWritten = 0;
            byte[] bytes = BitConverter.GetBytes(message.Length);
            byte[] numArray = new byte[bytes.Length + message.Length];
            Array.Copy((Array)bytes, (Array)numArray, bytes.Length);
            Array.Copy((Array)message, 0, (Array)numArray, bytes.Length, message.Length);
            if (!Redbox.HAL.Component.Model.Interop.Win32.WriteFile(this.PipeHandle, numArray, numArray.Length, out lpNumberOfBytesWritten, IntPtr.Zero))
            {
                LogHelper.Instance.Log(string.Format("[BasePipeChannel] Write failed: GetLastError() = {0}", (object)Marshal.GetLastWin32Error()), LogEntryType.Error);
                return false;
            }
            if (this.FlushOnWrite)
                BasePipeChannel.FlushFileBuffers(this.PipeHandle);
            return true;
        }

        public byte[] Read()
        {
            byte[] numArray = this.ReadBytes(this.MessageHeaderSize);
            return numArray == null || numArray.Length != this.MessageHeaderSize ? (byte[])null : this.ReadBytes(BitConverter.ToInt32(numArray, 0));
        }

        public byte[] Read(int timeout) => this.Read();

        public void Read(IIPCResponse response) => this.Read(response, 30000);

        public void Read(IIPCResponse response, int timeout)
        {
            byte[] rawResponse = this.Read();
            if (rawResponse == null)
                return;
            response.Accumulate(rawResponse);
        }

        public bool IsConnected { get; protected set; }

        protected abstract bool OnDisconnect();

        protected abstract bool OnConnect();

        protected byte[] ReadBytes(int count)
        {
            if (this.PipeHandle.IsInvalid)
                return (byte[])null;
            int lpNumberOfBytesRead = 0;
            byte[] lpBuffer = new byte[count];
            if (!Redbox.HAL.Component.Model.Interop.Win32.ReadFile(this.PipeHandle, lpBuffer, count, out lpNumberOfBytesRead, IntPtr.Zero))
            {
                LogHelper.Instance.Log(string.Format("PipeClientSession.ReadFile() returned false; error code = {0}", (object)Marshal.GetLastWin32Error()), LogEntryType.Error);
                return (byte[])null;
            }
            return lpNumberOfBytesRead == count ? lpBuffer : (byte[])null;
        }

        protected BasePipeChannel(string id)
        {
            this.FlushOnWrite = false;
            this.ID = id;
        }

        protected BasePipeChannel(SafeFileHandle handle, bool flushOnWrite, string id)
        {
            this.PipeHandle = handle;
            this.FlushOnWrite = flushOnWrite;
            this.ID = id;
        }

        protected BasePipeChannel(SafeFileHandle pipeHandle, string id)
          : this(pipeHandle, false, id)
        {
        }

        ~BasePipeChannel() => this.DisposeChannel(false);

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern bool FlushFileBuffers(SafeFileHandle handle);

        private void DisposeChannel(bool fromDispose)
        {
            if (this.Disposed)
                return;
            this.Disposed = true;
            this.Disconnect();
            LogHelper.Instance.Log(LogEntryType.Debug, "[{0}-{1}] OnDispose", (object)this.GetType().Name, (object)this.ID);
        }
    }
}
