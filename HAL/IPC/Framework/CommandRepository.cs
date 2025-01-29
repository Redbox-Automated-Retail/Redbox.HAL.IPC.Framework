using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Attributes;
using Redbox.HAL.Component.Model.Timers;
using Redbox.IPC.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Redbox.HAL.IPC.Framework
{
    [Command("command-repository")]
    [Description("The COMMAND-REPOSITORY command scans the execution directory of the cache service for .NET assembly files (.dll and .exe) that contain types marked with the CommandAttribute class.")]
    public class CommandRepository
    {
        private static readonly IDictionary<string, Type> m_commands = (IDictionary<string, Type>)new Dictionary<string, Type>();
        private static readonly IDictionary<Type, CommandInstance> m_instanceCache = (IDictionary<Type, CommandInstance>)new Dictionary<Type, CommandInstance>();

        public void Default(CommandContext context)
        {
            foreach (string installedCommand in CommandRepository.DiscoverInstalledCommands())
                context.Messages.Add("Installed Command '" + installedCommand + "'.");
        }

        internal static List<string> DiscoverInstalledCommands()
        {
            using (ExecutionTimer executionTimer = new ExecutionTimer())
            {
                List<string> stringList1 = new List<string>();
                string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                List<string> stringList2 = new List<string>();
                stringList2.AddRange((IEnumerable<string>)Directory.GetFiles(directoryName, "*.dll"));
                foreach (string assemblyFile in stringList2)
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(assemblyFile);
                        stringList1.AddRange((IEnumerable<string>)CommandRepository.FindCommands((IEnumerable<Type>)assembly.GetTypes()));
                    }
                    catch (BadImageFormatException ex)
                    {
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Instance.Log(string.Format("Unable to load assembly '{0}' to scan for commands.", (object)assemblyFile), ex);
                    }
                }
                LogHelper.Instance.Log(string.Format("Time to scan for commands: {0}", (object)executionTimer.Elapsed));
                return stringList1;
            }
        }

        internal static CommandInstance GetCommand(string name)
        {
            if (string.IsNullOrEmpty(name))
                return (CommandInstance)null;
            Type type;
            if (!CommandRepository.m_commands.TryGetValue(name.ToUpper(), out type))
                return (CommandInstance)null;
            CommandInstance command;
            if (!CommandRepository.m_instanceCache.TryGetValue(type, out command))
            {
                command = new CommandInstance()
                {
                    CommandType = type
                };
                if (Attribute.GetCustomAttribute((MemberInfo)type, typeof(DescriptionAttribute)) is DescriptionAttribute customAttribute1)
                    command.CommandDescription = customAttribute1.Description;
                foreach (MethodInfo method in type.GetMethods())
                {
                    if (Attribute.GetCustomAttribute((MemberInfo)method, typeof(CommandFormAttribute)) is CommandFormAttribute customAttribute5)
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length != 0 && parameters[0].ParameterType == typeof(CommandContext))
                        {
                            UsageAttribute customAttribute2 = Attribute.GetCustomAttribute((MemberInfo)method, typeof(UsageAttribute)) as UsageAttribute;
                            DescriptionAttribute customAttribute3 = Attribute.GetCustomAttribute((MemberInfo)method, typeof(DescriptionAttribute)) as DescriptionAttribute;
                            FormMethod formMethod = new FormMethod()
                            {
                                Method = method,
                                Usage = customAttribute2 != null ? customAttribute2.Template : string.Empty,
                                Description = customAttribute3 != null ? customAttribute3.Description : string.Empty
                            };
                            int num = 0;
                            foreach (ParameterInfo element in parameters)
                            {
                                if (element.ParameterType != typeof(CommandContext))
                                {
                                    FormMethodParameter formMethodParameter = new FormMethodParameter()
                                    {
                                        Parameter = element,
                                        Index = num++
                                    };
                                    CommandKeyValueAttribute customAttribute4 = Attribute.GetCustomAttribute(element, typeof(CommandKeyValueAttribute)) as CommandKeyValueAttribute;
                                    formMethodParameter.KeyName = element.Name.ToUpper();
                                    if (customAttribute4 != null)
                                    {
                                        formMethodParameter.MetaData = customAttribute4;
                                        if (customAttribute4.KeyName != null)
                                            formMethodParameter.KeyName = customAttribute4.KeyName.ToUpper();
                                        formMethod.ParameterCache[formMethodParameter.KeyName] = formMethodParameter;
                                    }
                                    else
                                        formMethod.ParameterCache[formMethodParameter.KeyName] = formMethodParameter;
                                }
                            }
                            command.FormMethodCache[customAttribute5.Name.ToUpper()] = formMethod;
                        }
                    }
                }
                MethodInfo method1 = type.GetMethod("Default", new Type[1]
                {
          typeof (CommandContext)
                });
                if (method1 != null)
                    command.FormMethodCache["Default"] = new FormMethod()
                    {
                        Method = method1
                    };
                CommandRepository.m_instanceCache[type] = command;
            }
            return command;
        }

        internal static void Register(IEnumerable<Type> types)
        {
            CommandRepository.FindCommands(types);
            LogHelper.Instance.Log("Installed commands:");
            foreach (string allCommand in (IEnumerable<string>)CommandRepository.AllCommands)
                LogHelper.Instance.Log(" Command '{0}'.", (object)allCommand);
        }

        internal static ICollection<string> AllCommands => CommandRepository.m_commands.Keys;

        private static List<string> FindCommands(IEnumerable<Type> types)
        {
            List<string> commands = new List<string>();
            foreach (Type type in types)
            {
                foreach (CommandAttribute customAttribute in (CommandAttribute[])type.GetCustomAttributes(typeof(CommandAttribute), false))
                {
                    if (customAttribute.Name != null)
                    {
                        string upper = customAttribute.Name.ToUpper();
                        commands.Add(upper);
                        CommandRepository.m_commands[upper] = type;
                    }
                }
            }
            return commands;
        }
    }
}
