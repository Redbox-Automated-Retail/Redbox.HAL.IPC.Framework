using Redbox.IPC.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Redbox.HAL.IPC.Framework
{
    [Command("help")]
    [Description("The HELP command, with no parameters, returns a list of all available commands. The HELP command with a single parameter matching the name of another command will display the help specifically for that command, if available.")]
    public class HelpCommand
    {
        public void Default(CommandContext context)
        {
            if (context.Parameters.Count == 0)
            {
                foreach (string allCommand in (IEnumerable<string>)CommandRepository.AllCommands)
                    context.Messages.Add(allCommand.ToUpper());
            }
            else
                context.ForEachSymbolDo((Action<string>)(each =>
                {
                    CommandInstance command = CommandRepository.GetCommand(each);
                    if (command == null)
                        return;
                    if (context.Messages.Count > 0)
                        context.Messages.Add(Environment.NewLine);
                    if (command.CommandDescription != null)
                        context.Messages.Add(command.CommandDescription);
                    foreach (string key in (IEnumerable<string>)command.FormMethodCache.Keys)
                    {
                        string usage = command.FormMethodCache[key].Usage;
                        string description = command.FormMethodCache[key].Description;
                        if (!string.IsNullOrEmpty(usage))
                        {
                            context.Messages.Add(usage);
                            if (!string.IsNullOrEmpty(description))
                            {
                                context.Messages.Add(description);
                                context.Messages.Add(Environment.NewLine);
                            }
                        }
                    }
                }));
        }
    }
}
