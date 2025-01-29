using System;

namespace Redbox.IPC.Framework
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string name) => this.Name = name;

        public string Name { get; set; }
    }
}
