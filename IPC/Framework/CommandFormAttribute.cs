using System;

namespace Redbox.IPC.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandFormAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
