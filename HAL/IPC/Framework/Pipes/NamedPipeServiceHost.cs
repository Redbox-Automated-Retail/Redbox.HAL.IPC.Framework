using Redbox.HAL.Component.Model;
using Redbox.HAL.IPC.Framework.Server;
using System;
using System.Threading;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    internal sealed class NamedPipeServiceHost : AbstractIPCServiceHost
    {
        private int m_identifier;

        protected override void OnStart()
        {
            LogHelper.Instance.Log("[NamedPipeServiceHost] Accepting connections on pipe {0}", (object)this.Protocol.GetPipeName());
            NamedPipeServer namedPipeServer = NamedPipeServer.Create(this.Protocol.GetPipeName());
            try
            {
                while (this.Alive)
                {
                    BasePipeChannel channel = namedPipeServer.WaitForClientConnect();
                    if (channel != null)
                    {
                        string id = Interlocked.Increment(ref this.m_identifier).ToString();
                        NamedPipeServerSession session = new NamedPipeServerSession(channel, (IIpcServiceHost)this, id);
                        this.Register((ISession)session);
                        ThreadPool.QueueUserWorkItem((WaitCallback)(o => session.Start()));
                    }
                }
                LogHelper.Instance.Log("[NamedPipeServiceHost] Exiting.");
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("[NamedPipeServiceHost] Caught an exception", ex);
            }
        }

        protected override void OnStop()
        {
            LogHelper.Instance.Log("[NamedPipeServiceHost] Stop on pipe {0}", (object)this.Protocol.GetPipeName());
            using (NamedPipeClientSession pipeClientSession = NamedPipeClientSession.MakeSession(this.Protocol, "Shutdown"))
                pipeClientSession.ConnectThrowOnError();
        }

        internal NamedPipeServiceHost(IIpcProtocol protocol, IHostInfo info)
          : base(protocol, info)
        {
        }
    }
}
