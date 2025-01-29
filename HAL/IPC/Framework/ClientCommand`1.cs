using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Compression;
using Redbox.HAL.Component.Model.Extensions;
using Redbox.HAL.Component.Model.Timers;
using Redbox.IPC.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Redbox.HAL.IPC.Framework
{
    public class ClientCommand<T> where T : ClientCommandResult, new()
    {
        private TextWriter m_logFile;
        private const string IPCLogsDirectory = "C:\\Program Files\\Redbox\\KioskLogs\\IPCFramework";
        private IPCProtocol m_protocol;

        public static T ExecuteCommand(IPCProtocol protocol, string command)
        {
            return new ClientCommand<T>(protocol, command).Execute();
        }

        public static T ExecuteCommand(IPCProtocol protocol, int? timeout, string command)
        {
            return new ClientCommand<T>(protocol, timeout, command).Execute();
        }

        public static T ExecuteCommand(IIpcClientSession session, string command)
        {
            return new ClientCommand<T>(command).Execute(session);
        }

        public T Execute()
        {
            using (IIpcClientSession clientSession = ClientSessionFactory.GetClientSession(this.m_protocol))
                return this.Execute(clientSession);
        }

        public T Execute(IIpcClientSession session)
        {
            using (ExecutionTimer executionTimer = new ExecutionTimer())
            {
                T obj = new T();
                obj.CommandText = this.CommandText;
                T result = obj;
                try
                {
                    int? timeout = this.Timeout;
                    if (timeout.HasValue)
                    {
                        IIpcClientSession ipcClientSession = session;
                        timeout = this.Timeout;
                        int num = timeout.Value;
                        ipcClientSession.Timeout = num;
                    }
                    if (!session.IsConnected)
                        session.ConnectThrowOnError();
                    List<string> stringList = session.ExecuteCommand(this.CommandText);
                    result.Success = session.IsStatusOK(stringList);
                    if (stringList.Count > 0)
                    {
                        result.StatusMessage = stringList[stringList.Count - 1];
                        stringList.RemoveAt(stringList.Count - 1);
                    }
                    result.CommandMessages.AddRange((IEnumerable<string>)stringList);
                    foreach (int convertMessage in ClientCommand<T>.ConvertMessages(result))
                    {
                        if (convertMessage < result.CommandMessages.Count)
                            result.CommandMessages.RemoveAt(convertMessage);
                    }
                }
                catch (SocketException ex)
                {
                    this.ErrorLogFile.WriteLine("ClientCommand.Execute() caught a socket exception; SocketErrorCode = {0}, Native = {1}", (object)ex.SocketErrorCode, (object)ex.NativeErrorCode);
                    this.ErrorLogFile.Flush();
                    this.OnConnectionFailure((Exception)ex, (ClientCommandResult)result);
                }
                catch (TimeoutException ex)
                {
                    this.ErrorLogFile.WriteLine("ClientCommand.Execute() caught a Timeout exception ( exception Message = {0} )", (object)ex.Message);
                    this.ErrorLogFile.Flush();
                    this.OnConnectionFailure((Exception)ex, (ClientCommandResult)result);
                }
                catch (Exception ex)
                {
                    this.OnUnhandledException(ex, (ClientCommandResult)result);
                }
                finally
                {
                    result.ExecutionTime = executionTimer.Elapsed;
                    this.ExecutionTime = new TimeSpan?(result.ExecutionTime);
                }
                return result;
            }
        }

        public override string ToString() => string.Format("Command: {0}", (object)this.CommandText);

        public string CommandText { get; internal set; }

        public TimeSpan? ExecutionTime { get; private set; }

        protected ClientCommand(IPCProtocol protocol, string command)
        {
            this.m_protocol = protocol;
            this.CommandText = command;
        }

        protected ClientCommand(IPCProtocol protocol, int? timeout, string command)
          : this(protocol, command)
        {
            this.Timeout = timeout;
        }

        protected ClientCommand(string command) => this.CommandText = command;

        protected virtual void OnConnectionFailure(Exception e, ClientCommandResult result)
        {
            result.Success = false;
            result.Errors.Add(Error.NewError("J001", string.Format("Unable to connect to command service on {0}:{1}.", (object)this.m_protocol.Host, (object)this.m_protocol.Port), "Check that the host and port values are correct and that the command service is running on the specified host."));
        }

        protected virtual void OnUnhandledException(Exception e, ClientCommandResult result)
        {
            result.Success = false;
            result.Errors.Add(Error.NewError("J999", "An unhandled exception was raised in ClientCommand.Execute.", e));
        }

        internal static List<int> ConvertMessages(T result)
        {
            List<int> intList = new List<int>();
            for (int index1 = 0; index1 < result.CommandMessages.Count; ++index1)
            {
                if (result.CommandMessages[index1].StartsWith("|*"))
                {
                    intList.Add(index1);
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(result.CommandMessages[index1]);
                    for (int index2 = index1 + 1; index2 < result.CommandMessages.Count; ++index2)
                    {
                        intList.Add(index2);
                        stringBuilder.AppendLine(result.CommandMessages[index2]);
                        if (result.CommandMessages[index2].EndsWith("*|"))
                            break;
                    }
                    stringBuilder.Replace("|*", string.Empty);
                    stringBuilder.Replace("*|", string.Empty);
                    string str = stringBuilder.ToString();
                    string codeFromBrackets = StringExtensions.ExtractCodeFromBrackets(str, "[", "]");
                    if (codeFromBrackets != null)
                    {
                        string[] strArray = str.Substring(codeFromBrackets.Length + 2).Split("|".ToCharArray());
                        if (strArray[0].IndexOf("WARNING") != -1)
                            result.Errors.Add(Error.NewWarning(codeFromBrackets, strArray[0].Trim(), strArray.Length == 2 ? strArray[1].Trim() : string.Empty));
                        else
                            result.Errors.Add(Error.NewError(codeFromBrackets, strArray[0].Trim(), strArray.Length == 2 ? strArray[1].Trim() : string.Empty));
                    }
                }
                else if (result.CommandMessages[index1].StartsWith("LZMA|"))
                {
                    ICompressionAlgorithm algorithm = CompressionAlgorithmFactory.GetAlgorithm(CompressionType.LZMA);
                    result.CommandMessages[index1] = Encoding.ASCII.GetString(algorithm.Decompress(StringExtensions.Base64ToBytes(result.CommandMessages[index1].Substring(5))));
                }
                else if (result.CommandMessages[index1].StartsWith("GZIP|"))
                {
                    ICompressionAlgorithm algorithm = CompressionAlgorithmFactory.GetAlgorithm(CompressionType.GZip);
                    result.CommandMessages[index1] = Encoding.ASCII.GetString(algorithm.Decompress(StringExtensions.Base64ToBytes(result.CommandMessages[index1].Substring(5))));
                }
            }
            return intList;
        }

        internal int? Timeout { get; set; }

        private TextWriter ErrorLogFile
        {
            get
            {
                if (this.m_logFile == null)
                {
                    if (!Directory.Exists("C:\\Program Files\\Redbox\\KioskLogs\\IPCFramework"))
                    {
                        try
                        {
                            Directory.CreateDirectory("C:\\Program Files\\Redbox\\KioskLogs\\IPCFramework");
                        }
                        catch
                        {
                        }
                    }
                    Process currentProcess = Process.GetCurrentProcess();
                    string path = Path.Combine("C:\\Program Files\\Redbox\\KioskLogs\\IPCFramework", string.Format("IPCClientErrors-{0}-{1}.log", (object)currentProcess.ProcessName, (object)currentProcess.Id));
                    try
                    {
                        this.m_logFile = (TextWriter)new StreamWriter((Stream)File.Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                    }
                    catch
                    {
                        this.m_logFile = (TextWriter)StreamWriter.Null;
                    }
                }
                return this.m_logFile;
            }
        }
    }
}
