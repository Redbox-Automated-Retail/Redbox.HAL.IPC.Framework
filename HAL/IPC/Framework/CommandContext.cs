using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;

namespace Redbox.HAL.IPC.Framework
{
    public class CommandContext
    {
        public readonly IDictionary<string, string> Parameters = (IDictionary<string, string>)new Dictionary<string, string>();
        public readonly MessageList Messages = new MessageList();
        public readonly ErrorList Errors = new ErrorList();

        public ISession Session { get; internal set; }

        public IMessageSink MessageSink { get; internal set; }

        internal static bool IsSymbol(string value)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(":") == -1;
        }

        internal void ForEachSymbolDo(Action<string> action)
        {
            this.ForEachSymbolDo(action, (string[])null);
        }

        internal void ForEachNamedParameterDo(Action<string> action)
        {
            this.ForEachNamedParameterDo(action, (string[])null);
        }

        internal void ForEachSymbolDo(Action<string> action, string[] exclusions)
        {
            foreach (string key in (IEnumerable<string>)this.Parameters.Keys)
            {
                if (CommandContext.IsSymbol(key))
                {
                    string tempKey = key;
                    if (exclusions == null || !Array.Exists<string>(exclusions, (Predicate<string>)(eachExclusion => eachExclusion == tempKey)))
                        action(key);
                }
            }
        }

        internal void ForEachNamedParameterDo(Action<string> action, string[] exclusions)
        {
            foreach (string key in (IEnumerable<string>)this.Parameters.Keys)
            {
                if (!CommandContext.IsSymbol(key))
                {
                    string tempKey = key;
                    if (exclusions == null || !Array.Exists<string>(exclusions, (Predicate<string>)(eachExclusion => eachExclusion == tempKey)))
                        action(key);
                }
            }
        }
    }
}
