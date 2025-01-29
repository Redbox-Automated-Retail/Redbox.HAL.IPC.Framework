using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Redbox.HAL.IPC.Framework
{
    public abstract class AbstractServerSession : ISession, IMessageSink
    {
        protected readonly IIpcServiceHost ServiceHost;
        protected readonly StringBuilder ReadBuilder = new StringBuilder();
        protected readonly string Identifier;
        private readonly string Welcome;
        private readonly List<string> MessageSinkQueue = new List<string>();
        private readonly object m_syncLock = new object();

        public void Start()
        {
            LogHelper.Instance.Log("[AbstractServerSession-{0}] Start", (object)this.Identifier);
            RedboxChannelDecorator.Write(this.Channel, this.Welcome);
            this.CommLoop();
        }

        public bool Send(string message)
        {
            string msg = string.Format("[MSG] {0}", (object)message);
            if (this.LogDetailedMessages)
                LogHelper.Instance.Log(msg);
            lock (this.m_syncLock)
                this.MessageSinkQueue.Add(msg);
            return true;
        }

        public IDictionary<string, object> Properties { get; private set; }

        public event EventHandler Disconnect;

        public bool LogDetailedMessages { get; private set; }

        protected AbstractServerSession(IIpcServiceHost host, string id)
          : this(host, id, LogHelper.Instance.IsLevelEnabled(LogEntryType.Debug))
        {
        }

        protected AbstractServerSession(IIpcServiceHost host, string id, bool logDetails)
        {
            this.ServiceHost = host;
            this.LogDetailedMessages = logDetails;
            this.Properties = (IDictionary<string, object>)new Dictionary<string, object>();
            this.Identifier = id;
            this.Welcome = string.Format("Welcome! {0}, Version {1}, {2}", (object)host.HostInfo.Product, (object)host.HostInfo.Version, (object)host.HostInfo.Copyright);
        }

        protected abstract bool OnSessionEnd();

        protected void CommLoop()
        {
            while (true)
            {
                using (ServerResponse response = new ServerResponse())
                {
                    this.Channel.Read((IIPCResponse)response);
                    if (!response.IsComplete)
                    {
                        this.QuitSession(false);
                        break;
                    }
                    if (this.LogDetailedMessages)
                        LogHelper.Instance.Log("[AbstractServerSession-{0}] Received command = '{1}'", (object)this.Identifier, (object)response.Command);
                    if (response.Command.StartsWith("quit", StringComparison.CurrentCultureIgnoreCase))
                    {
                        this.QuitSession(true);
                        break;
                    }
                    CommandResult commandResult = CommandService.Instance.Execute((ISession)this, response.Command);
                    ProtocolHelper.FormatErrors(commandResult.Errors, (List<string>)commandResult.Messages);
                    this.FlushSinkQueue((ICollection<string>)commandResult.Messages);
                    string line = commandResult.ToString();
                    if (this.LogDetailedMessages)
                        LogHelper.Instance.Log("[AbstractServerSession - {0}]  Response = {1}", (object)this.Identifier, (object)line);
                    RedboxChannelDecorator.Write(this.Channel, line);
                }
            }
        }

        protected bool QuitSession(bool sendBye)
        {
            if (sendBye)
                RedboxChannelDecorator.Write(this.Channel, "Goodbye!");
            if (this.Disconnect != null)
                this.Disconnect((object)this, EventArgs.Empty);
            bool flag = false;
            try
            {
                this.OnSessionEnd();
                if (this.LogDetailedMessages)
                    LogHelper.Instance.Log("[AbstractServerSession - {0}] Session end.", (object)this.Identifier);
                flag = true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("Caught exception in QuitSession", ex);
            }
            try
            {
                this.ServiceHost.Unregister((ISession)this);
                return flag;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("QuitSession: unable to unregister session.", ex);
                return false;
            }
        }

        protected abstract IIPCChannel Channel { get; }

        private void FlushSinkQueue(ICollection<string> messages)
        {
            lock (this.m_syncLock)
            {
                while (this.MessageSinkQueue.Count > 0)
                {
                    string messageSink = this.MessageSinkQueue[0];
                    this.MessageSinkQueue.RemoveAt(0);
                    messages.Add(messageSink);
                }
            }
        }
    }
}
