using Redbox.HAL.Component.Model;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;

namespace Redbox.HAL.IPC.Framework.Sockets
{
    internal sealed class TcpClientSession : Redbox.HAL.IPC.Framework.Client.ClientSession
    {
        private IIPCChannel m_channel;
        private TcpClient TransportClient;
        private Stream Stream;
        private readonly int m_clientPort;

        protected override void OnDispose()
        {
            try
            {
                this.TransportClient.Close();
            }
            catch (Exception ex)
            {
            }
        }

        protected override bool OnConnect()
        {
            this.TransportClient = new TcpClient(this.Protocol.Host, this.m_clientPort);
            Stream stream = (Stream)this.TransportClient.GetStream();
            if (!this.Protocol.IsSecure)
            {
                stream.ReadTimeout = this.Timeout;
            }
            else
            {
                SslStream sslStream = new SslStream(stream, false, (RemoteCertificateValidationCallback)((sender, certificate, chain, sslPolicyErrors) => true));
                sslStream.ReadTimeout = this.Timeout;
                sslStream.WriteTimeout = this.Timeout;
                sslStream.AuthenticateAsClient(this.Protocol.Host);
                stream = (Stream)sslStream;
            }
            this.Stream = stream;
            this.m_channel = (IIPCChannel)new AsyncChannel(stream, this.TransportClient.ReceiveBufferSize, this.Identifier);
            return this.m_channel.Connect();
        }

        protected override IIPCChannel Channel => this.m_channel;

        internal TcpClientSession(IIpcProtocol protocol, int port, string identifier)
          : base(protocol, identifier)
        {
            this.m_clientPort = port;
        }
    }
}
