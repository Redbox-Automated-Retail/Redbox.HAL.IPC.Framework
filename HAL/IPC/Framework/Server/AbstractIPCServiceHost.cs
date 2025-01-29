using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Threading;
using System.Collections.Generic;

namespace Redbox.HAL.IPC.Framework.Server
{
    internal abstract class AbstractIPCServiceHost : IIpcServiceHost
    {
        private readonly AtomicFlag AliveFlag = new AtomicFlag(false);
        private readonly object m_sessionLockObject = new object();
        private readonly List<ISession> m_sessions = new List<ISession>();

        public void Start()
        {
            this.Alive = true;
            this.OnStart();
        }

        public void Stop()
        {
            this.Alive = false;
            this.OnStop();
        }

        public void Unregister(ISession session)
        {
            int num = 0;
            lock (this.m_sessionLockObject)
            {
                this.m_sessions.Remove(session);
                num = this.m_sessions.Count;
            }
            if (!this.LogDetailedMessages)
                return;
            LogHelper.Instance.Log("[IPCServiceHost] Unregister - there are {0} active server sessions.", (object)num);
        }

        public void Register(ISession session)
        {
            int num = 0;
            lock (this.m_sessionLockObject)
            {
                this.m_sessions.Add(session);
                num = this.m_sessions.Count;
            }
            if (!this.LogDetailedMessages)
                return;
            LogHelper.Instance.Log("[IPCServiceHost] Register - there are {0} active server sessions.", (object)num);
        }

        public bool Alive
        {
            get => this.AliveFlag.IsSet;
            set
            {
                if (value)
                    this.AliveFlag.Set();
                else
                    this.AliveFlag.Clear();
            }
        }

        public IIpcProtocol Protocol { get; private set; }

        public IHostInfo HostInfo { get; private set; }

        public bool LogDetailedMessages { get; set; }

        protected abstract void OnStart();

        protected abstract void OnStop();

        protected AbstractIPCServiceHost(IIpcProtocol protocol, IHostInfo info)
        {
            this.Protocol = protocol;
            this.HostInfo = info;
        }
    }
}
