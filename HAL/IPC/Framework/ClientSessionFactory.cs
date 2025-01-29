using Redbox.HAL.Component.Model;
using Redbox.HAL.IPC.Framework.Pipes;
using Redbox.IPC.Framework;
using System;
using System.Threading;

namespace Redbox.HAL.IPC.Framework
{
    public static class ClientSessionFactory
    {
        private static int m_session;

        public static IIpcClientSession GetClientSession(IPCProtocol protocol)
        {
            string identifier = string.Format("Session-{0}", (object)Interlocked.Increment(ref ClientSessionFactory.m_session));
            if (protocol.Channel == ChannelType.NamedPipe)
                return (IIpcClientSession)NamedPipeClientSession.MakeSession((IIpcProtocol)protocol, identifier);
            if (protocol.Channel == ChannelType.Socket)
            {
                int result;
                if (int.TryParse(protocol.Port, out result))
                    return (IIpcClientSession)new TcpClientSession(protocol, result);
                LogHelper.Instance.Log(string.Format("TCP port specification {0} is incorrect; check your URI and try again.", (object)protocol.Port), LogEntryType.Fatal);
                throw new Exception("Port specification in URI is incorrect.");
            }
            LogHelper.Instance.Log(LogEntryType.Error, "Unrecognized channel type {0}; please fix your protocol string.", (object)protocol.Channel);
            return (IIpcClientSession)null;
        }
    }
}
