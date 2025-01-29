using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Interop;
using System;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    public class ClientPipeChannel : BasePipeChannel
    {
        private readonly string PipePath;

        public ClientPipeChannel(string pipeName, string id)
          : base(id)
        {
            this.PipePath = "\\\\.\\PIPE\\" + pipeName;
        }

        protected override bool OnDisconnect()
        {
            LogHelper.Instance.Log(LogEntryType.Debug, "[ClientPipeChannel-{0}] OnDisconnect", (object)this.ID);
            this.PipeHandle.Close();
            return true;
        }

        protected override bool OnConnect()
        {
            if (this.IsConnected)
                return true;
            this.PipeHandle = Win32.CreateFile(this.PipePath, Win32.AccessFlags.GENERIC_READ_WRITE, Win32.ShareFlags.NONE, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
            return this.IsConnected = !this.PipeHandle.IsInvalid;
        }
    }
}
