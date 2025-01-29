using Redbox.HAL.Component.Model;
using System;
using System.Net;
using System.Net.Sockets;

namespace Redbox.HAL.IPC.Framework
{
    public class TcpServiceHost : IPCServiceHost
    {
        private TcpListener m_listener;
        private int m_listenerPort;

        protected override void OnStart()
        {
            this.Alive = true;
            IPAddress address1;
            IPAddress address2;
            if (IPAddress.TryParse(this.Protocol.Host, out address1))
            {
                address2 = address1;
            }
            else
            {
                address2 = IPAddressHelper.GetAddressForHostName(this.Protocol.Host);
                if (address2 == null)
                {
                    LogHelper.Instance.Log(string.Format("Unable to bind to host named '{0}' on port {1},  ensure there is a valid network interface associated to this host and that the designated port is available.", (object)this.Protocol.Host, (object)this.Protocol.Port), LogEntryType.Fatal);
                    return;
                }
            }
            if (!int.TryParse(this.Protocol.Port, out this.m_listenerPort))
            {
                LogHelper.Instance.Log(string.Format("Unable to create a listener on port '{0}'; maybe your uri is wrong?", (object)this.Protocol.Port), LogEntryType.Fatal);
            }
            else
            {
                this.m_listener = new TcpListener(new IPEndPoint(address2, this.m_listenerPort));
                this.m_listener.Start();
                LogHelper.Instance.Log("TCP Server: start processing incoming requests on address {0}.", (object)address2);
                Statistics.Instance.ServerStartTime = DateTime.Now;
                while (this.Alive)
                {
                    try
                    {
                        TcpServerSession tcpServerSession = new TcpServerSession(this.m_listener.AcceptTcpClient(), (IPCServiceHost)this);
                        this.Register((ISession)tcpServerSession);
                        tcpServerSession.Start();
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode != SocketError.Interrupted)
                            LogHelper.Instance.Log("An unhandled socket exception occurred", (Exception)ex);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Instance.Log("An unhandled exception was raised.", ex);
                    }
                }
                LogHelper.Instance.Log("End processing incoming requests.");
            }
        }

        protected override void OnStop()
        {
            this.Alive = false;
            this.m_listener.Stop();
        }

        internal TcpServiceHost(IIpcProtocol protocol, IHostInfo info)
          : base(protocol, info)
        {
        }
    }
}
