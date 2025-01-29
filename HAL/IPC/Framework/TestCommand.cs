using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Attributes;
using Redbox.IPC.Framework;
using System;
using System.ComponentModel;
using System.Text;

namespace Redbox.HAL.IPC.Framework
{
    [Command("ipctest")]
    public sealed class TestCommand
    {
        [CommandForm(Name = "test-ipc-xfer")]
        [Usage("ipctest test-ipc-xfer size: payloadSize")]
        [Description("")]
        public void TestIPCTransfer(CommandContext context, [CommandKeyValue(IsRequired = true, KeyName = "size")] int payloadSize)
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < payloadSize; ++index)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26.0 * random.NextDouble() + 65.0)));
                stringBuilder.Append(ch);
            }
            string msg = stringBuilder.ToString();
            LogHelper.Instance.Log("String: ");
            LogHelper.Instance.Log(msg);
            context.Messages.Add(msg);
        }

        [CommandForm(Name = "test-comm")]
        [Usage("ipctest test-comm")]
        [Description("")]
        public void TestIPCTransfer(CommandContext context) => context.Messages.Add("ACK");
    }
}
