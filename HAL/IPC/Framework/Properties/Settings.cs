using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Redbox.HAL.IPC.Framework.Properties
{
    [CompilerGenerated]
    [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = (Settings)SettingsBase.Synchronized((SettingsBase)new Settings());

        public static Settings Default => Settings.defaultInstance;

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("HALService7001")]
        public string PipeName => (string)this[nameof(PipeName)];
    }
}
