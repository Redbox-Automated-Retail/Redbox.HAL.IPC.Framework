using Redbox.IPC.Framework;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;

namespace Redbox.HAL.IPC.Framework
{
    public sealed class TcpClientSession : ClientSession
    {
        private Stream m_stream;
        private TcpClient m_transportClient;
        private int m_clientPort;

        protected override void OnConnectThrowOnError()
        {
            this.TransportClient = new TcpClient(this.Protocol.Host, this.m_clientPort);
            this.ConsumeMessages();
            this.IsConnected = true;
        }

        protected internal override bool IsConnectionAvailable()
        {
            return !this.TransportClient.Client.Poll(10, SelectMode.SelectRead) || this.TransportClient.Client.Available != 0;
        }

        protected internal override void CustomClose()
        {
            try
            {
                this.Stream.Close();
                this.TransportClient.Close();
            }
            catch (Exception ex)
            {
            }
        }

        protected override bool CanRead()
        {
            return this.Stream.CanRead && this.TransportClient.Client.Available > 0;
        }

        protected override int GetAvailableData() => this.TransportClient.Client.Available;

        protected override Stream Stream
        {
            get
            {
                if (this.m_stream == null)
                {
                    if (!this.Protocol.IsSecure)
                    {
                        this.m_stream = (Stream)this.TransportClient.GetStream();
                        this.m_stream.ReadTimeout = this.Timeout;
                    }
                    else
                    {
                        SslStream sslStream1 = new SslStream((Stream)this.TransportClient.GetStream(), false, (RemoteCertificateValidationCallback)((sender, certificate, chain, sslPolicyErrors) => true));
                        sslStream1.ReadTimeout = this.Timeout;
                        sslStream1.WriteTimeout = this.Timeout;
                        SslStream sslStream2 = sslStream1;
                        sslStream2.AuthenticateAsClient(this.Protocol.Host);
                        this.m_stream = (Stream)sslStream2;
                    }
                }
                return this.m_stream;
            }
        }

        internal TcpClientSession(IPCProtocol protocol, int port)
          : base(protocol)
        {
            this.m_clientPort = port;
        }

        private TcpClient TransportClient
        {
            get => this.m_transportClient;
            set
            {
                this.m_transportClient = value;
                if (this.m_transportClient == null)
                    return;
                this.ReadBuffer = new byte[this.m_transportClient.ReceiveBufferSize];
            }
        }
    }
}
