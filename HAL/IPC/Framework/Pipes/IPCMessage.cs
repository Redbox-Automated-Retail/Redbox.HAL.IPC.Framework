using Redbox.HAL.Component.Model;
using System;
using System.Text;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    internal sealed class IPCMessage : IIPCMessage
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(" IPC Message ");
            stringBuilder.AppendFormat("   Type       = {0}{1}", (object)this.Type.ToString(), (object)Environment.NewLine);
            stringBuilder.AppendFormat("   Severity   = {0}{1}", (object)this.Severity.ToString(), (object)Environment.NewLine);
            stringBuilder.AppendFormat("   Message    = {0}{1}", (object)this.Message, (object)Environment.NewLine);
            stringBuilder.AppendFormat("   UID        = {0}{1}", (object)this.UID.ToString(), (object)Environment.NewLine);
            stringBuilder.AppendFormat("   Timestamp  = {0}{1}", (object)this.Timestamp.ToString(), (object)Environment.NewLine);
            return stringBuilder.ToString();
        }

        public MessageType Type { get; internal set; }

        public MessageSeverity Severity { get; internal set; }

        public string Message { get; internal set; }

        public Guid UID { get; internal set; }

        public DateTime Timestamp { get; internal set; }
    }
}
