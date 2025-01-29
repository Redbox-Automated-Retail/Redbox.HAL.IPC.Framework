using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Redbox.HAL.IPC.Framework
{
    public sealed class BatchCommandRunner : ISession, IMessageSink
    {
        private readonly ErrorList m_errorList = new ErrorList();
        private readonly StreamReader m_reader;

        public BatchCommandRunner(StreamReader reader)
        {
            this.m_reader = reader;
            this.LogDetailedMessages = LogHelper.Instance.IsLevelEnabled(LogEntryType.Debug);
        }

        public bool Send(string message) => true;

        public static ErrorList ExecuteStartupFiles()
        {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.txt");
            ErrorList errorList = new ErrorList();
            foreach (string fileName in files)
                BatchCommandRunner.ExecuteFile(fileName, errorList);
            return errorList;
        }

        public static void ExecuteFile(string fileName, ErrorList errorList)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                BatchCommandRunner batchCommandRunner = new BatchCommandRunner(reader);
                batchCommandRunner.Start();
                if (batchCommandRunner.Errors.Count <= 0)
                    return;
                errorList.AddRange((IEnumerable<Error>)batchCommandRunner.Errors);
                batchCommandRunner.Errors.Clear();
            }
        }

        public ErrorList ExecuteFile()
        {
            this.Start();
            return this.Errors;
        }

        public void Start()
        {
            StringBuilder builder = new StringBuilder();
            CommandResult commandResult;
            do
            {
                do
                {
                    string str;
                    do
                    {
                        builder.Length = 0;
                        if (this.Read(builder))
                            str = builder.ToString();
                        else
                            goto label_11;
                    }
                    while (str.Length <= 0);
                    if (this.LogDetailedMessages)
                        LogHelper.Instance.Log(str);
                    if (str.IndexOf("quit", 0, StringComparison.CurrentCultureIgnoreCase) == -1)
                        commandResult = CommandService.Instance.Execute((ISession)this, str);
                    else
                        goto label_12;
                }
                while (commandResult == null);
                ProtocolHelper.FormatErrors(commandResult.Errors, (List<string>)commandResult.Messages);
                if (this.LogDetailedMessages)
                {
                    LogEntryType type = commandResult.Errors.Count > 0 ? LogEntryType.Error : LogEntryType.Info;
                    LogHelper.Instance.Log(commandResult.ToString(), type);
                }
            }
            while (!commandResult.Errors.ContainsError());
            goto label_10;
        label_11:
            return;
        label_12:
            return;
        label_10:
            this.Errors.AddRange((IEnumerable<Error>)commandResult.Errors);
        }

        public event EventHandler Disconnect
        {
            add
            {
            }
            remove
            {
            }
        }

        public bool LogDetailedMessages { get; set; }

        internal ErrorList Errors => this.m_errorList;

        private bool Read(StringBuilder builder)
        {
            string str = this.m_reader.ReadLine();
            if (str == null)
                return false;
            builder.Append(str);
            return true;
        }
    }
}
