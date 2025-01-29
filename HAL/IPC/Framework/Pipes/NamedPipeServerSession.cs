using Redbox.HAL.Component.Model;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    internal sealed class NamedPipeServerSession : AbstractServerSession
    {
        private readonly IIPCChannel m_channel;

        protected override bool OnSessionEnd()
        {
            this.Channel.Dispose();
            return true;
        }

        protected override IIPCChannel Channel => this.m_channel;

        internal NamedPipeServerSession(BasePipeChannel channel, IIpcServiceHost host, string id)
          : base(host, id)
        {
            this.m_channel = (IIPCChannel)channel;
        }
    }
}
