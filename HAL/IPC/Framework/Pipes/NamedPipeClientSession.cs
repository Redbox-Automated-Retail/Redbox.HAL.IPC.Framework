using Redbox.HAL.Component.Model;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    public sealed class NamedPipeClientSession : Redbox.HAL.IPC.Framework.Client.ClientSession
    {
        private readonly BasePipeChannel m_channel;
        private readonly string ID;

        public static NamedPipeClientSession MakeSession(IIpcProtocol protocol, string identifier)
        {
            return new NamedPipeClientSession(identifier, protocol);
        }

        protected override bool OnConnect() => this.m_channel.Connect();

        protected override IIPCChannel Channel => (IIPCChannel)this.m_channel;

        private NamedPipeClientSession(string id, IIpcProtocol protocol)
          : base(protocol, id)
        {
            this.ID = id;
            this.m_channel = (BasePipeChannel)new ClientPipeChannel(protocol.GetPipeName(), id);
        }
    }
}
