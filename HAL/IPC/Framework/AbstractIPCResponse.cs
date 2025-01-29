using Redbox.HAL.Component.Model;
using System;
using System.Text;

namespace Redbox.HAL.IPC.Framework
{
    internal abstract class AbstractIPCResponse : IIPCResponse, IDisposable
    {
        protected readonly bool LogDetails;
        protected readonly StringBuilder ReadBuilder = new StringBuilder();
        private bool Disposed;

        public void Dispose()
        {
            if (this.Disposed)
                return;
            this.Disposed = true;
            this.OnDispose(true);
            GC.SuppressFinalize((object)this);
        }

        public bool Accumulate(byte[] rawResponse)
        {
            return this.Accumulate(rawResponse, 0, rawResponse.Length);
        }

        public bool Accumulate(byte[] bytes, int start, int length)
        {
            this.ReadBuilder.Append(Encoding.ASCII.GetString(bytes, start, length));
            return this.IsComplete = this.OnTestResponse();
        }

        public void Clear()
        {
            this.IsComplete = false;
            if (this.ReadBuilder.Length > 0)
                this.ReadBuilder.Length = 0;
            this.OnClear();
        }

        public bool IsComplete { get; protected set; }

        protected abstract void OnClear();

        protected virtual void OnDispose(bool fromDispose)
        {
        }

        protected abstract bool OnTestResponse();

        protected string GetNextBufferLine()
        {
            string str1 = this.ReadBuilder.ToString();
            int num = str1.IndexOf(Environment.NewLine);
            if (-1 == num)
                return string.Empty;
            string str2 = str1.Substring(0, num + Environment.NewLine.Length);
            this.ReadBuilder.Remove(0, num + Environment.NewLine.Length);
            return str2.Trim();
        }

        protected bool BufferHasMoreLines()
        {
            return this.ReadBuilder.ToString().IndexOf(Environment.NewLine) == -1;
        }

        protected AbstractIPCResponse()
          : this(false)
        {
        }

        protected AbstractIPCResponse(bool logDetails) => this.LogDetails = logDetails;
    }
}
