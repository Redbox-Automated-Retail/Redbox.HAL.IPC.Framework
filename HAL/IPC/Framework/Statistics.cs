using Redbox.HAL.Core;
using System;

namespace Redbox.HAL.IPC.Framework
{
    public class Statistics
    {
        public long NumberOfBytesSent;
        public DateTime ServerStartTime;
        public long NumberOfBytesReceived;
        public long NumberOfCommandsExecuted;
        public TimeSpan TotalCommandExecutionTime;
        public TimeSpan MinimumCommandExecutionTime;
        public TimeSpan MaximumCommandExecutionTime;
        private readonly object m_syncObject = new object();

        public static Statistics Instance => Singleton<Statistics>.Instance;

        public void TrackCommandStatistics(TimeSpan executionTime)
        {
            lock (this.m_syncObject)
            {
                ++this.NumberOfCommandsExecuted;
                this.TotalCommandExecutionTime = this.TotalCommandExecutionTime.Add(executionTime);
                if (executionTime < this.MinimumCommandExecutionTime)
                {
                    this.MinimumCommandExecutionTime = executionTime;
                }
                else
                {
                    if (!(executionTime > this.MaximumCommandExecutionTime))
                        return;
                    this.MaximumCommandExecutionTime = executionTime;
                }
            }
        }

        public string Host => Environment.MachineName;

        public TimeSpan ServerUpTime => DateTime.Now - this.ServerStartTime;

        public long AverageNumberOfBytesSent
        {
            get
            {
                long numberOfBytesSent = 0;
                if (this.NumberOfCommandsExecuted > 0L && this.NumberOfBytesSent > 0L)
                    numberOfBytesSent = this.NumberOfBytesSent / this.NumberOfCommandsExecuted;
                return numberOfBytesSent;
            }
        }

        public long AverageNumberOfBytesReceived
        {
            get
            {
                long numberOfBytesReceived = 0;
                if (this.NumberOfCommandsExecuted > 0L && this.NumberOfBytesReceived > 0L)
                    numberOfBytesReceived = this.NumberOfBytesReceived / this.NumberOfCommandsExecuted;
                return numberOfBytesReceived;
            }
        }

        public TimeSpan AverageCommandExecutionTime
        {
            get
            {
                long ticks = 0;
                if (this.NumberOfCommandsExecuted > 0L)
                    ticks = this.TotalCommandExecutionTime.Ticks / this.NumberOfCommandsExecuted;
                return new TimeSpan(ticks);
            }
        }

        private Statistics()
        {
        }
    }
}
