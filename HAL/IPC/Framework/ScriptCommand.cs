using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Attributes;
using Redbox.IPC.Framework;
using System;

namespace Redbox.HAL.IPC.Framework
{
    [Command("script")]
    public class ScriptCommand
    {
        [CommandForm(Name = "execute")]
        [Usage("SCRIPT execute path: 'C:\\temp\\my-script.txt'")]
        public void Execute(CommandContext context, [CommandKeyValue(IsRequired = true)] string path)
        {
            try
            {
                ErrorList errorList = new ErrorList();
                BatchCommandRunner.ExecuteFile(path, errorList);
            }
            catch (Exception ex)
            {
                context.Errors.Add(Error.NewError("S999", "An unhandled exception was raised in ScriptCommmand.Execute.", ex));
            }
        }
    }
}
