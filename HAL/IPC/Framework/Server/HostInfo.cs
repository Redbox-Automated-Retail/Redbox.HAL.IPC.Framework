using Redbox.HAL.Component.Model;
using System;
using System.Reflection;

namespace Redbox.HAL.IPC.Framework.Server
{
    internal sealed class HostInfo : IHostInfo
    {
        public string Product { get; private set; }

        public string Version { get; private set; }

        public string Copyright { get; private set; }

        internal HostInfo(Assembly a)
        {
            this.Product = this.GetProductName(a);
            this.Version = this.GetVersion(a);
            this.Copyright = this.GetProductName(a);
        }

        private string GetProductName(Assembly assembly)
        {
            return ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)))?.Product;
        }

        private string GetCopyright(Assembly assembly)
        {
            return ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute)))?.Copyright;
        }

        private string GetVersion(Assembly assembly) => assembly.GetName().Version.ToString();
    }
}
