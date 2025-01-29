using Redbox.HAL.Component.Model;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Redbox.HAL.IPC.Framework
{
    public abstract class IPCServiceHost : IIpcServiceHost
    {
        private const int StateRunning = 1;
        private const int StateStopped = 0;
        private int m_aliveState;
        private readonly object m_sessionLockObject = new object();
        private readonly IList<ISession> m_sessions = (IList<ISession>)new List<ISession>();

        public void Start() => this.OnStart();

        public void Stop() => this.OnStop();

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
            LogHelper.Instance.Log(string.Format("Unregister server session - there are {0} active server sessions.", (object)num), LogEntryType.Debug);
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
            LogHelper.Instance.Log(string.Format("Register server session - there are {0} active server sessions.", (object)num), LogEntryType.Debug);
        }

        public bool Alive
        {
            get => Thread.VolatileRead(ref this.m_aliveState) == 1;
            set => Interlocked.Exchange(ref this.m_aliveState, value ? 1 : 0);
        }

        public IIpcProtocol Protocol { get; protected set; }

        public IHostInfo HostInfo { get; private set; }

        public X509Certificate2 Certificate { get; set; }

        public SslProtocols EncryptionProtocol { get; set; }

        public bool LogDetailedMessages { get; set; }

        protected abstract void OnStart();

        protected abstract void OnStop();

        protected IPCServiceHost(IIpcProtocol protocol, IHostInfo info)
        {
            this.EncryptionProtocol = SslProtocols.None;
            this.Protocol = protocol;
            this.HostInfo = info;
        }
    }
}
