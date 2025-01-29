using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Redbox.HAL.IPC.Framework
{
    public abstract class ServerSession : ISession, IMessageSink
    {
        private readonly IPCServiceHost ServiceHost;
        private readonly List<string> MessageSinkQueue = new List<string>();
        private readonly StringBuilder ReadBuilder = new StringBuilder();
        private readonly IDictionary<string, string> m_context = (IDictionary<string, string>)new Dictionary<string, string>();
        private readonly object m_syncLock = new object();

        public ServerSession(IPCServiceHost serviceHost, int bufferSize)
        {
            this.ServiceHost = serviceHost;
            this.ReadBuffer = new byte[bufferSize];
            this.LogDetailedMessages = false;
        }

        public void Start()
        {
            this.Write(string.Format("Welcome! {0}, Version {1}, {2}", (object)this.ServiceHost.HostInfo.Product, (object)this.ServiceHost.HostInfo.Version, (object)this.ServiceHost.HostInfo.Copyright));
            this.Read();
        }

        public bool Send(string message)
        {
            string str = string.Format("[MSG] {0}", (object)message);
            if (this.LogDetailedMessages)
                LogHelper.Instance.Log(str);
            this.EnqueueSinkMessage(str);
            return true;
        }

        public IDictionary<string, string> Context => this.m_context;

        public event EventHandler Disconnect;

        public bool LogDetailedMessages { get; set; }

        public bool CloseClients() => this.CloseClientsInternal();

        public bool CloseStreams() => this.CloseStreamsInternal();

        protected abstract bool CloseClientsInternal();

        protected abstract bool CloseStreamsInternal();

        protected abstract int ReadBufferSize { get; }

        protected Stream Stream { get; set; }

        protected byte[] ReadBuffer { get; set; }

        protected void NegotiateSecurityLayer(Stream stream)
        {
            if (!this.IsSecure)
            {
                this.Stream = stream;
            }
            else
            {
                SslStream sslStream1 = new SslStream(stream, false);
                sslStream1.ReadTimeout = 30000;
                sslStream1.WriteTimeout = 30000;
                SslStream sslStream2 = sslStream1;
                sslStream2.AuthenticateAsServer((X509Certificate)this.ServiceHost.Certificate, false, SslProtocols.Tls, true);
                this.Stream = (Stream)sslStream2;
            }
        }

        private void RaiseDisconnect()
        {
            if (this.Disconnect == null)
                return;
            this.Disconnect((object)this, EventArgs.Empty);
        }

        private void Read()
        {
            try
            {
                this.Stream.BeginRead(this.ReadBuffer, 0, this.ReadBuffer.Length, new AsyncCallback(this.EndReadCallback), (object)null);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("ServerSession.Read caught an exception", ex);
                this.QuitSession((string)null);
            }
        }

        private void Write(string s)
        {
            if (!this.Stream.CanWrite)
                return;
            try
            {
                if (!s.EndsWith(Environment.NewLine))
                    s = string.Format("{0}{1}", (object)s, (object)Environment.NewLine);
                this.Stream.Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
                Interlocked.Add(ref Statistics.Instance.NumberOfBytesSent, (long)s.Length);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("There was an unhandled exception writing to the client session.", ex);
                this.QuitSession((string)null);
            }
        }

        private void EndReadCallback(IAsyncResult result)
        {
            try
            {
                int count = this.Stream.EndRead(result);
                if (count == 0)
                    return;
                Interlocked.Add(ref Statistics.Instance.NumberOfBytesReceived, (long)count);
                this.ReadBuilder.Append(Encoding.ASCII.GetString(this.ReadBuffer, 0, count));
                if (!this.HasMoreBufferLines())
                    ThreadPool.QueueUserWorkItem((WaitCallback)(o => this.ProcessReadBuilder()));
                else
                    this.Read();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("ClientSession.Execute has caught an unhandled exception.", ex);
                this.QuitSession((string)null);
            }
        }

        private void ProcessReadBuilder()
        {
            while (true)
            {
                string str;
                do
                {
                    string nextBufferLine = this.GetNextBufferLine();
                    if (nextBufferLine == string.Empty && this.HasMoreBufferLines())
                    {
                        this.Read();
                        return;
                    }
                    if (nextBufferLine.StartsWith("quit", StringComparison.CurrentCultureIgnoreCase))
                    {
                        this.QuitSession("Goodbye!");
                        return;
                    }
                    CommandResult commandResult = CommandService.Instance.Execute((ISession)this, nextBufferLine);
                    ProtocolHelper.FormatErrors(commandResult.Errors, (List<string>)commandResult.Messages);
                    this.FlushSinkQueue((ICollection<string>)commandResult.Messages);
                    str = commandResult.ToString();
                    this.Write(str);
                }
                while (!this.LogDetailedMessages);
                LogHelper.Instance.Log(str, LogEntryType.Debug);
            }
        }

        private bool QuitSession(string msg)
        {
            if (msg != null)
            {
                if (this.LogDetailedMessages)
                    LogHelper.Instance.Log(msg, LogEntryType.Debug);
                this.Write(msg);
            }
            this.RaiseDisconnect();
            bool flag = false;
            try
            {
                this.CloseClients();
                this.CloseStreams();
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

        private void EnqueueSinkMessage(string message)
        {
            lock (this.m_syncLock)
                this.MessageSinkQueue.Add(message);
        }

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

        private string GetNextBufferLine()
        {
            string str1 = this.ReadBuilder.ToString();
            int num = str1.IndexOf(Environment.NewLine);
            if (num == -1)
                return string.Empty;
            string str2 = str1.Substring(0, num + Environment.NewLine.Length);
            this.ReadBuilder.Remove(0, num + Environment.NewLine.Length);
            return str2.Trim();
        }

        private bool HasMoreBufferLines()
        {
            return this.ReadBuilder.ToString().IndexOf(Environment.NewLine) == -1;
        }

        private bool IsSecure
        {
            get
            {
                return this.ServiceHost.EncryptionProtocol != SslProtocols.None && this.ServiceHost.Certificate != null;
            }
        }
    }
}
