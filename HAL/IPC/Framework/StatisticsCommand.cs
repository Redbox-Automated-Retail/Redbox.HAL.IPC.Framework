using Redbox.HAL.Component.Model.Attributes;
using Redbox.IPC.Framework;
using System.Reflection;

namespace Redbox.HAL.IPC.Framework
{
    [Command("statistics")]
    public class StatisticsCommand
    {
        [CommandForm(Name = "show")]
        [Usage("STATISTICS show")]
        public void Show(CommandContext context)
        {
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public;
            foreach (FieldInfo field in typeof(Statistics).GetFields(bindingAttr))
            {
                string str = string.Format("{0} = {1}", (object)field.Name, field.GetValue((object)Statistics.Instance));
                context.Messages.Add(str);
            }
            foreach (PropertyInfo property in typeof(Statistics).GetProperties(bindingAttr))
            {
                string str = string.Format("{0} = {1}", (object)property.Name, property.GetValue((object)Statistics.Instance, (object[])null));
                context.Messages.Add(str);
            }
        }
    }
}
