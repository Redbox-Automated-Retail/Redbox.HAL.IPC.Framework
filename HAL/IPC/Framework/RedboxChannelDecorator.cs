using Redbox.HAL.Component.Model;
using System;
using System.Text;
using System.Threading;

namespace Redbox.HAL.IPC.Framework
{
    internal static class RedboxChannelDecorator
    {
        internal static bool Write(IIPCChannel channel, string line)
        {
            if (!line.EndsWith(Environment.NewLine))
                line = string.Format("{0}{1}", (object)line, (object)Environment.NewLine);
            byte[] bytes = Encoding.ASCII.GetBytes(line);
            int num = channel.Write(bytes) ? 1 : 0;
            if (num == 0)
                return num != 0;
            Interlocked.Add(ref Statistics.Instance.NumberOfBytesSent, (long)bytes.Length);
            return num != 0;
        }
    }
}
