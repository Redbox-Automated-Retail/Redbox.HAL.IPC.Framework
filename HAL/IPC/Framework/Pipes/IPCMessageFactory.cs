using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Redbox.HAL.IPC.Framework.Pipes
{
    public sealed class IPCMessageFactory : IIPCMessageFactory
    {
        public IIPCMessage Create(MessageType p, MessageSeverity s, string message)
        {
            return (IIPCMessage)new IPCMessage()
            {
                Type = p,
                Severity = s,
                Message = message,
                UID = Guid.NewGuid(),
                Timestamp = DateTime.Now
            };
        }

        public IIPCMessage CreateAck(Guid guid)
        {
            return (IIPCMessage)new IPCMessage()
            {
                Type = MessageType.Ack,
                Severity = MessageSeverity.None,
                Message = string.Empty,
                UID = guid,
                Timestamp = DateTime.Now
            };
        }

        public IIPCMessage CreateNack(Guid guid)
        {
            return (IIPCMessage)new IPCMessage()
            {
                Type = MessageType.Nack,
                Severity = MessageSeverity.None,
                Message = string.Empty,
                UID = guid,
                Timestamp = DateTime.Now
            };
        }

        public IIPCMessage Read(IIPCChannel channel)
        {
            byte[] numArray1 = channel.Read();
            int num = 4;
            int startIndex1 = 0;
            int int32_1 = BitConverter.ToInt32(numArray1, startIndex1);
            int sourceIndex = startIndex1 + num;
            byte[] numArray2 = new byte[int32_1];
            Array.Copy((Array)numArray1, sourceIndex, (Array)numArray2, 0, int32_1);
            Guid guid = new Guid(numArray2);
            int startIndex2 = sourceIndex + int32_1;
            MessageType int32_2 = (MessageType)BitConverter.ToInt32(numArray1, startIndex2);
            int startIndex3 = startIndex2 + num;
            MessageSeverity int32_3 = (MessageSeverity)BitConverter.ToInt32(numArray1, startIndex3);
            int startIndex4 = startIndex3 + num;
            int int32_4 = BitConverter.ToInt32(numArray1, startIndex4);
            int index = startIndex4 + num;
            string str = Encoding.ASCII.GetString(numArray1, index, int32_4);
            int startIndex5 = index + int32_4;
            DateTime dateTime = DateTime.FromBinary(BitConverter.ToInt64(numArray1, startIndex5));
            return (IIPCMessage)new IPCMessage()
            {
                Type = int32_2,
                Severity = int32_3,
                Message = str,
                UID = guid,
                Timestamp = dateTime
            };
        }

        public bool Write(IIPCMessage msg, IIPCChannel channel)
        {
            List<byte> byteList = new List<byte>();
            try
            {
                byte[] byteArray = msg.UID.ToByteArray();
                byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes(byteArray.Length));
                byteList.AddRange((IEnumerable<byte>)byteArray);
                byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes((int)msg.Type));
                byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes((int)msg.Severity));
                byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes(msg.Message.Length));
                byteList.AddRange((IEnumerable<byte>)Encoding.ASCII.GetBytes(msg.Message));
                byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes(msg.Timestamp.Ticks));
                return channel.Write(byteList.ToArray());
            }
            finally
            {
                byteList.Clear();
            }
        }
    }
}
