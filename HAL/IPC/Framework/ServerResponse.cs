namespace Redbox.HAL.IPC.Framework
{
    internal sealed class ServerResponse : AbstractIPCResponse
    {
        protected override void OnClear() => this.Command = string.Empty;

        protected override bool OnTestResponse()
        {
            this.ReadBuilder.ToString();
            if (this.BufferHasMoreLines())
                return false;
            while (true)
            {
                string nextBufferLine = this.GetNextBufferLine();
                if (!(nextBufferLine == string.Empty))
                    this.Command = nextBufferLine;
                else
                    break;
            }
            return true;
        }

        internal ServerResponse(bool logDetails)
          : base(logDetails)
        {
            this.Command = string.Empty;
        }

        internal ServerResponse()
          : this(true)
        {
        }

        internal string Command { get; private set; }
    }
}
