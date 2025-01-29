using Microsoft.Win32.SafeHandles;
using Redbox.HAL.Component.Model;
using System;
using System.Runtime.InteropServices;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    public sealed class NamedPipeServer : IDisposable
    {
        private bool Disposed;
        private readonly string PipeName;
        private readonly string PipePath;
        private readonly Redbox.HAL.Component.Model.Interop.Win32.SecurityDescriptor m_securityDescriptor;
        private readonly Redbox.HAL.Component.Model.Interop.Win32.SecurityAttributes m_securityAttributes;
        private readonly IntPtr m_securityDescriptorPointer = IntPtr.Zero;
        private readonly IntPtr m_securityAttributesPointer = IntPtr.Zero;
        private readonly int BufferSize = 4096;
        private const int PIPE_FLAGS_OVERLAPPED = 1073741824;
        private const int PIPE_ACCESS_OUTBOUND = 2;
        private const int PIPE_ACCESS_DUPLEX = 3;
        private const int PIPE_ACCESS_INBOUND = 1;
        private const int PIPE_WAIT = 0;
        private const int PIPE_NOWAIT = 1;
        private const int PIPE_READMODE_BYTE = 0;
        private const int PIPE_READMODE_MESSAGE = 2;
        private const int PIPE_TYPE_BYTE = 0;
        private const int PIPE_TYPE_MESSAGE = 4;
        private const int PIPE_CLIENT_END = 0;
        private const int PIPE_SERVER_END = 1;
        private const int PIPE_UNLIMITED_INSTANCES = 255;
        private const uint NMPWAIT_WAIT_FOREVER = 4294967295;
        private const uint NMPWAIT_NOWAIT = 1;
        private const uint NMPWAIT_USE_DEFAULT_WAIT = 0;
        private static IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);
        private const uint SECURITY_DESCRIPTOR_REVISION = 1;

        public static NamedPipeServer Create(string pipeName) => new NamedPipeServer(pipeName);

        public BasePipeChannel WaitForClientConnect()
        {
            SafeFileHandle namedPipe = NamedPipeServer.CreateNamedPipe(this.PipePath, 3U, 0U, (uint)byte.MaxValue, (uint)this.BufferSize, (uint)this.BufferSize, uint.MaxValue, IntPtr.Zero);
            if (namedPipe.IsInvalid)
                return (BasePipeChannel)null;
            int lastWin32Error = NamedPipeServer.ConnectNamedPipe(namedPipe, IntPtr.Zero) ? 0 : Marshal.GetLastWin32Error();
            return 535 == lastWin32Error || lastWin32Error == 0 ? (BasePipeChannel)ServerPipeChannel.Make(namedPipe) : (BasePipeChannel)null;
        }

        ~NamedPipeServer() => this.OnDispose(false);

        public void Dispose()
        {
            this.OnDispose(true);
            GC.SuppressFinalize((object)this);
        }

        private void OnDispose(bool disposing)
        {
            if (this.Disposed)
                return;
            Marshal.FreeCoTaskMem(this.m_securityDescriptorPointer);
            Marshal.FreeCoTaskMem(this.m_securityAttributesPointer);
            this.Disposed = true;
            LogHelper.Instance.Log("[NamedPipeServer] OnDispose");
        }

        private NamedPipeServer(string pipeName)
        {
            this.m_securityDescriptor = new Redbox.HAL.Component.Model.Interop.Win32.SecurityDescriptor();
            Redbox.HAL.Component.Model.Interop.Win32.InitializeSecurityDescriptor(ref this.m_securityDescriptor, 1U);
            Redbox.HAL.Component.Model.Interop.Win32.SetSecurityDescriptorDacl(ref this.m_securityDescriptor, true, IntPtr.Zero, false);
            this.m_securityDescriptorPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf((object)this.m_securityDescriptor));
            Marshal.StructureToPtr((object)this.m_securityDescriptor, this.m_securityDescriptorPointer, false);
            this.m_securityAttributes = new Redbox.HAL.Component.Model.Interop.Win32.SecurityAttributes()
            {
                nLength = Marshal.SizeOf((object)this.m_securityDescriptor),
                lpSecurityDescriptor = this.m_securityDescriptorPointer,
                bInheritHandle = 1
            };
            this.m_securityAttributesPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf((object)this.m_securityAttributes));
            Marshal.StructureToPtr((object)this.m_securityAttributes, this.m_securityAttributesPointer, false);
            this.PipeName = pipeName;
            this.PipePath = "\\\\.\\PIPE\\" + this.PipeName;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateNamedPipe(
          string pipeName,
          uint dwOpenMode,
          uint dwPipeMode,
          uint nMaxInstances,
          uint nOutBufferSize,
          uint nInBufferSize,
          uint nDefaultTimeOut,
          IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ConnectNamedPipe(SafeFileHandle hNamedPipe, IntPtr lpOverlapped);
    }
}
