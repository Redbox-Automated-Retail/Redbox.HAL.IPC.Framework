using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Redbox.HAL.IPC.Framework
{
    public sealed class CommandResult
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string message in (List<string>)this.Messages)
                stringBuilder.AppendLine(message);
            if (this.Success)
            {
                stringBuilder.Append("203 Command completed successfully.");
            }
            else
            {
                stringBuilder.Append("545 Command failed.");
                if (this.ExtendedErrorMessage != null)
                    stringBuilder.AppendFormat(" {0}", (object)this.ExtendedErrorMessage);
            }
            stringBuilder.AppendLine(" (Execution Time = " + this.ExecutionTime.ToString() + ")");
            return stringBuilder.ToString();
        }

        public bool Success { get; internal set; }

        public TimeSpan ExecutionTime { get; internal set; }

        public string ExtendedErrorMessage { get; internal set; }

        public MessageList Messages { get; private set; }

        public ErrorList Errors { get; private set; }

        internal CommandResult()
        {
            this.Messages = new MessageList();
            this.Errors = new ErrorList();
        }
    }
}
