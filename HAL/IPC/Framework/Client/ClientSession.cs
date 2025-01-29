using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace Redbox.HAL.IPC.Framework.Client
{
    public abstract class ClientSession : IIpcClientSession, IDisposable
    {
        protected readonly bool LogDetailed;
        private int? m_timeout;

        public void Dispose()
        {
            if (this.IsDisposed)
                return;
            if (this.LogDetailed)
                LogHelper.Instance.Log("[ClientSession] Disposing id {0}", (object)this.Identifier);
            this.IsDisposed = true;
            try
            {
                this.ExecuteCommand("quit");
                this.Channel.Dispose();
            }
            catch (IOException ex)
            {
            }
            finally
            {
                this.OnDispose();
            }
        }

        public void ConnectThrowOnError()
        {
            this.IsConnected = this.OnConnect();
            if (!this.IsConnected)
                throw new TimeoutException(string.Format("Failed to connect on {0}", (object)this.Protocol.ToString()));
            this.ConsumeMessages();
        }

        public bool IsStatusOK(List<string> messages)
        {
            bool flag = false;
            if (messages.Count > 0)
                flag = messages[messages.Count - 1].StartsWith("203 Command");
            return flag;
        }

        public List<string> ExecuteCommand(string command)
        {
            if (this.LogDetailed)
                LogHelper.Instance.Log("[ClientSession-{0}] executing {1}", (object)this.Identifier, (object)command);
            RedboxChannelDecorator.Write(this.Channel, command);
            MessageList messageList = this.ConsumeMessages();
            if (this.LogDetailed)
            {
                foreach (string str in (List<string>)messageList)
                    LogHelper.Instance.Log("[ClientSession-{0}]  Response msg {1}", (object)this.Identifier, (object)str);
            }
            return (List<string>)messageList;
        }

        public int Timeout
        {
            get => this.m_timeout ?? 30000;
            set => this.m_timeout = new int?(value);
        }

        public bool IsConnected { get; private set; }

        public string Identifier { get; private set; }

        public IIpcProtocol Protocol { get; protected set; }

        public bool IsDisposed { get; private set; }

        public event Action<string> ServerEvent;

        protected virtual void OnDispose()
        {
        }

        protected abstract bool OnConnect();

        protected MessageList ConsumeMessages()
        {
            ClientResponse response = new ClientResponse(this, this.LogDetailed);
            this.Channel.Read((IIPCResponse)response);
            if (!response.IsComplete)
            {
                response.Clear();
                ErrorList errors = new ErrorList();
                errors.Add(Error.NewError("J888", string.Format("Timeout threshold {0} exceeded.", (object)this.Timeout), "Reissue the command when the service is not as busy."));
                ProtocolHelper.FormatErrors(errors, (List<string>)response.Messages);
                response.Messages.Add("545 Command failed.");
            }
            return response.Messages;
        }

        protected ClientSession(IIpcProtocol protocol, string id, bool logDetails)
        {
            this.Protocol = protocol;
            this.Identifier = id;
            this.LogDetailed = logDetails;
        }

        protected ClientSession(IIpcProtocol protocol, string id)
          : this(protocol, id, LogHelper.Instance.IsLevelEnabled(LogEntryType.Debug))
        {
        }

        protected abstract IIPCChannel Channel { get; }

        internal void OnServerEvent(string line)
        {
            if (this.ServerEvent == null)
                return;
            this.ServerEvent(line);
        }

        internal bool SupportsEvents => this.ServerEvent != null;
    }
}
