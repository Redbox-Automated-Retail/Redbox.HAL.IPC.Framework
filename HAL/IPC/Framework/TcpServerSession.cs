using System.IO;
using System.Net.Sockets;

namespace Redbox.HAL.IPC.Framework
{
    public class TcpServerSession : ServerSession
    {
        public TcpServerSession(TcpClient client, IPCServiceHost serviceHost)
          : base(serviceHost, client.ReceiveBufferSize)
        {
            this.TransportClient = client;
            this.TransportStream = client.GetStream();
            this.NegotiateSecurityLayer((Stream)this.TransportStream);
        }

        protected override int ReadBufferSize => 1024;

        protected override bool CloseClientsInternal()
        {
            this.TransportClient.Close();
            return true;
        }

        protected override bool CloseStreamsInternal()
        {
            this.Stream.Dispose();
            return true;
        }

        private TcpClient TransportClient { get; set; }

        private NetworkStream TransportStream { get; set; }
    }
}
