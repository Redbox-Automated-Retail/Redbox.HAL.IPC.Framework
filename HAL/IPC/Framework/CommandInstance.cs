using System;
using System.Collections.Generic;

namespace Redbox.HAL.IPC.Framework
{
    internal class CommandInstance
    {
        public readonly IDictionary<string, FormMethod> FormMethodCache = (IDictionary<string, FormMethod>)new Dictionary<string, FormMethod>();

        public object GetInstance() => Activator.CreateInstance(this.CommandType);

        public bool HasDefault() => this.FormMethodCache.ContainsKey("Default");

        public bool HasOnlyDefault() => this.FormMethodCache.Count == 1 && this.HasDefault();

        public void InvokeDefault(
          CommandResult result,
          CommandContext context,
          CommandTokenizer tokenizer)
        {
            if (!this.HasDefault())
                return;
            this.FormMethodCache["Default"].Invoke(result, context, tokenizer, this.GetInstance());
        }

        public FormMethod GetMethod(string formName)
        {
            if (string.IsNullOrEmpty(formName))
                return (FormMethod)null;
            string upper = formName.ToUpper();
            return this.FormMethodCache.ContainsKey(upper) ? this.FormMethodCache[upper] : (FormMethod)null;
        }

        public Type CommandType { get; set; }

        public string CommandDescription { get; set; }
    }
}
