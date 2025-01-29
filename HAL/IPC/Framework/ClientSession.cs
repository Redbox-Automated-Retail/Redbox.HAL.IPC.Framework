using Redbox.HAL.Component.Model;
using Redbox.IPC.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Redbox.HAL.IPC.Framework
{
    public abstract class ClientSession : IIpcClientSession, IDisposable
    {
        private int? m_timeout;
        private StringBuilder m_readBuilder;
        private readonly AutoResetEvent m_resetEvent = new AutoResetEvent(false);

        public virtual void Dispose()
        {
            this.Close();
            this.m_resetEvent.Close();
            this.IsDisposed = true;
            if (this.Disposed == null)
                return;
            this.Disposed((object)this, EventArgs.Empty);
        }

        public void ConnectThrowOnError() => this.OnConnectThrowOnError();

        public void Close()
        {
            if (!this.IsConnected)
                return;
            try
            {
                this.ExecuteCommand("quit");
            }
            catch (IOException ex)
            {
            }
            finally
            {
                this.IsConnected = false;
                this.CustomClose();
            }
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
            this.Write(command);
            return this.ConsumeMessages();
        }

        public int Timeout
        {
            get => this.m_timeout ?? 30000;
            set => this.m_timeout = new int?(value);
        }

        public bool IsConnected { get; internal set; }

        public IIpcProtocol Protocol { get; private set; }

        public bool IsDisposed { get; private set; }

        public event EventHandler Disposed;

        public event Action<string> ServerEvent;

        protected abstract void OnConnectThrowOnError();

        protected List<string> ConsumeMessages()
        {
            if (this.ReadBuilder.Length > 0)
                this.ReadBuilder.Length = 0;
            List<string> messages = new List<string>();
            this.Read(messages);
            if (!this.m_resetEvent.WaitOne(this.Timeout))
            {
                messages.Clear();
                ErrorList errors = new ErrorList();
                errors.Add(Error.NewError("J888", string.Format("Timeout threshold {0} exceeded.", (object)this.Timeout), "Reissue the command when the service is not as busy."));
                ProtocolHelper.FormatErrors(errors, messages);
                messages.Add("545 Command failed.");
            }
            return messages;
        }

        protected ClientSession(IPCProtocol protocol) => this.Protocol = (IIpcProtocol)protocol;

        protected internal abstract bool IsConnectionAvailable();

        protected internal abstract void CustomClose();

        protected abstract int GetAvailableData();

        protected abstract bool CanRead();

        protected abstract Stream Stream { get; }

        protected byte[] ReadBuffer { get; set; }

        private void Read(List<string> messages)
        {
            try
            {
                this.Stream.BeginRead(this.ReadBuffer, 0, this.ReadBuffer.Length, new AsyncCallback(this.EndReadCallback), (object)messages);
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log("ClientSession.Read caught an I/O exception; maybe server is down?");
                this.SetEvent();
            }
        }

        private void Write(string s)
        {
            try
            {
                if (!this.Stream.CanWrite)
                    return;
                if (!s.EndsWith(Environment.NewLine))
                    s = string.Format("{0}{1}", (object)s, (object)Environment.NewLine);
                this.Stream.Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log("ClientSession.Write caught an I/O exception; maybe server is down?");
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

        private bool BufferHasMoreLines()
        {
            return this.ReadBuilder.ToString().IndexOf(Environment.NewLine) == -1;
        }

        private void EndReadCallback(IAsyncResult result)
        {
            try
            {
                int count = this.Stream.EndRead(result);
                if (count == 0)
                {
                    this.SetEvent();
                }
                else
                {
                    List<string> asyncState = (List<string>)result.AsyncState;
                    string str = Encoding.ASCII.GetString(this.ReadBuffer, 0, count);
                    this.ReadBuilder.Append(str);
                    if (str.IndexOf(Environment.NewLine) == -1)
                        this.Read(asyncState);
                    else if (!this.ProcessReadBuilder((ICollection<string>)asyncState))
                        this.Read(asyncState);
                    else
                        this.SetEvent();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("ClientSession.EndRead caught an exception; maybe server is down?");
                this.SetEvent();
            }
        }

        private bool ProcessReadBuilder(ICollection<string> messages)
        {
            string nextBufferLine;
            do
            {
                nextBufferLine = this.GetNextBufferLine();
                if (nextBufferLine == string.Empty && this.BufferHasMoreLines())
                    return false;
                if (nextBufferLine.StartsWith("[MSG]"))
                {
                    if (this.ServerEvent != null)
                    {
                        int num = nextBufferLine.IndexOf("]");
                        if (num != -1)
                            this.ServerEvent(nextBufferLine.Substring(num + 1).Trim());
                    }
                }
                else
                    messages.Add(nextBufferLine);
            }
            while (!nextBufferLine.StartsWith("Welcome!") && !nextBufferLine.StartsWith("Goodbye!") && !nextBufferLine.StartsWith("545 Command") && !nextBufferLine.StartsWith("203 Command"));
            return true;
        }

        private StringBuilder ReadBuilder
        {
            get
            {
                if (this.m_readBuilder == null)
                    this.m_readBuilder = new StringBuilder();
                return this.m_readBuilder;
            }
        }

        private bool SetEvent()
        {
            try
            {
                this.m_resetEvent.Set();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log("Caught an exception setting the event.", ex);
                return false;
            }
        }
    }
}
