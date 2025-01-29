using Redbox.HAL.Component.Model;
using System.IO;
using System.Net.Sockets;

namespace Redbox.HAL.IPC.Framework.Sockets
{
    public sealed class TcpServerSession : AbstractServerSession
    {
        private readonly IIPCChannel m_channel;
        private readonly TcpClient TransportClient;

        public TcpServerSession(TcpClient client, IIpcServiceHost serviceHost, string id)
          : base(serviceHost, id)
        {
            this.TransportClient = client;
            this.m_channel = (IIPCChannel)new AsyncChannel((Stream)client.GetStream(), client.ReceiveBufferSize, id, true);
        }

        protected override bool OnSessionEnd()
        {
            if (this.LogDetailedMessages)
                LogHelper.Instance.Log("[TcpServerSession-{0}] Session end.", (object)this.Identifier);
            this.TransportClient.Close();
            this.Channel.Dispose();
            return true;
        }

        protected override IIPCChannel Channel => this.m_channel;
    }
}
