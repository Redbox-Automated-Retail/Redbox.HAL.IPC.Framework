using Redbox.HAL.Component.Model;

namespace Redbox.HAL.IPC.Framework.Client
{
    internal sealed class ClientResponse : AbstractIPCResponse
    {
        private readonly ClientSession Session;

        protected override void OnClear() => this.Messages.Clear();

        protected override bool OnTestResponse()
        {
            string nextBufferLine;
            do
            {
                nextBufferLine = this.GetNextBufferLine();
                if (nextBufferLine == string.Empty && this.BufferHasMoreLines())
                    return false;
                if (this.LogDetails)
                    LogHelper.Instance.Log("[ClientResponse] Processing Message = {0}", (object)nextBufferLine);
                if (nextBufferLine.StartsWith("[MSG]"))
                {
                    if (this.Session.SupportsEvents)
                    {
                        int num = nextBufferLine.IndexOf("]");
                        if (num != -1)
                            this.Session.OnServerEvent(nextBufferLine.Substring(num + 1).Trim());
                    }
                }
                else
                    this.Messages.Add(nextBufferLine);
            }
            while (!nextBufferLine.StartsWith("Welcome!") && !nextBufferLine.StartsWith("Goodbye!") && !nextBufferLine.StartsWith("545 Command") && !nextBufferLine.StartsWith("203 Command"));
            return true;
        }

        internal ClientResponse(ClientSession session)
          : this(session, true)
        {
        }

        internal ClientResponse(ClientSession session, bool logDetailed)
          : base(logDetailed)
        {
            this.Session = session;
            this.Messages = new MessageList();
        }

        internal MessageList Messages { get; private set; }
    }
}
