using Redbox.HAL.Component.Model;
using Redbox.HAL.IPC.Framework.Pipes;
using System;
using System.Reflection;

namespace Redbox.HAL.IPC.Framework.Server
{
    public sealed class IpcServiceHostFactory : IIpcServiceHostFactory
    {
        private readonly IpcHostVersion Version;

        public IIpcServiceHost Create(IIpcProtocol protocol) => throw new NotImplementedException();

        public IIpcServiceHost Create(IIpcProtocol protocol, IHostInfo info)
        {
            if (protocol.Channel == ChannelType.NamedPipe)
                return (IIpcServiceHost)new NamedPipeServiceHost(protocol, info);
            if (protocol.Channel == ChannelType.Socket)
                return this.CreateTcpHost(protocol, info);
            LogHelper.Instance.Log(LogEntryType.Error, "Unsupported channel type '{0}' specified; try using sockets or pipes in your protocol uri.", (object)protocol.Channel);
            return (IIpcServiceHost)null;
        }

        public IHostInfo Create(Assembly a) => (IHostInfo)new HostInfo(a);

        public IpcServiceHostFactory()
          : this(IpcHostVersion.Legacy)
        {
        }

        public IpcServiceHostFactory(IpcHostVersion version) => this.Version = version;

        private IIpcServiceHost CreateTcpHost(IIpcProtocol protocol, IHostInfo info)
        {
            return IpcHostVersion.Modern != this.Version ? (IIpcServiceHost)new Redbox.HAL.IPC.Framework.TcpServiceHost(protocol, info) : (IIpcServiceHost)new Redbox.HAL.IPC.Framework.Sockets.TcpServiceHost(protocol, info);
        }
    }
}
