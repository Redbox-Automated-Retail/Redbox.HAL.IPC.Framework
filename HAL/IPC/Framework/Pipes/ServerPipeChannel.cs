using Microsoft.Win32.SafeHandles;
using Redbox.HAL.Component.Model;
using System.Runtime.InteropServices;
using System.Threading;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    internal sealed class ServerPipeChannel : BasePipeChannel
    {
        private static int m_id;

        public static ServerPipeChannel Make(SafeFileHandle handle)
        {
            return new ServerPipeChannel(handle, Interlocked.Increment(ref ServerPipeChannel.m_id).ToString());
        }

        protected override bool OnDisconnect()
        {
            LogHelper.Instance.Log(LogEntryType.Debug, "[ServerPipeChannel-{0}] OnDisconnect", (object)this.ID);
            BasePipeChannel.FlushFileBuffers(this.PipeHandle);
            ServerPipeChannel.DisconnectNamedPipe(this.PipeHandle);
            this.PipeHandle.Close();
            return true;
        }

        protected override bool OnConnect()
        {
            LogHelper.Instance.Log("[ServerPipeChannel-{0}] Connect() improperly called.", (object)this.ID);
            return false;
        }

        private ServerPipeChannel(SafeFileHandle pipeHandle, string id)
          : base(pipeHandle, id)
        {
            this.IsConnected = !pipeHandle.IsInvalid;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DisconnectNamedPipe(SafeFileHandle hHandle);
    }
}
