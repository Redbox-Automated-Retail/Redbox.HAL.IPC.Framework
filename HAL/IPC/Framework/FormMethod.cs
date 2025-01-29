using Redbox.HAL.Component.Model;
using Redbox.HAL.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Redbox.HAL.IPC.Framework
{
    internal class FormMethod
    {
        public readonly IDictionary<string, FormMethodParameter> ParameterCache = (IDictionary<string, FormMethodParameter>)new Dictionary<string, FormMethodParameter>();
        private DynamicMethod m_dynamicMethod;
        private FormMethodHandler m_methodHandler;
        private List<FormMethodParameter> m_orderedParameters;
        private List<FormMethodParameter> m_requiredParameters;

        public void Invoke(
          CommandResult result,
          CommandContext context,
          CommandTokenizer tokenizer,
          object commandInstance)
        {
            object[] values = new object[this.OrderedParameters.Count + 1];
            values[0] = (object)context;
            int num = 1;
            foreach (FormMethodParameter orderedParameter in this.OrderedParameters)
            {
                KeyValuePair keyValuePair = tokenizer.Tokens.GetKeyValuePair(orderedParameter.KeyName);
                if (keyValuePair == null)
                {
                    values[num++] = (object)null;
                }
                else
                {
                    try
                    {
                        values[num++] = orderedParameter.ConvertValue(keyValuePair.Value);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(Error.NewError("S999", string.Format("Unable to convert key '{0}:' to type '{1}'.", (object)keyValuePair.Key, (object)orderedParameter.Parameter.ParameterType.Name), ex));
                    }
                }
            }
            if (result.Errors.ContainsError())
                return;
            try
            {
                this.InvokeMethod(commandInstance, values);
            }
            catch (Exception ex)
            {
                result.Errors.Add(Error.NewError("S999", string.Format("Execution of command form method '{0}.{1}' failed.", (object)commandInstance.GetType().Name, (object)this.Method.Name), ex));
            }
        }

        public bool ValidateParameters(CommandResult result, CommandTokenizer tokenizer)
        {
            foreach (FormMethodParameter requiredParameter in this.RequiredParameters)
            {
                if (tokenizer.Tokens.GetKeyValuePair(requiredParameter.KeyName) == null)
                    result.Errors.Add(Error.NewError("S001", string.Format("The named parameter '{0}:' is required.", (object)requiredParameter.KeyName.ToLower()), "Submit the command specifying the missing named parameter."));
            }
            return !result.Errors.ContainsError();
        }

        public string Usage { get; set; }

        public MethodInfo Method { get; set; }

        public string Description { get; set; }

        public List<FormMethodParameter> OrderedParameters
        {
            get
            {
                if (this.m_orderedParameters == null)
                {
                    this.m_orderedParameters = new List<FormMethodParameter>((IEnumerable<FormMethodParameter>)this.ParameterCache.Values);
                    this.m_orderedParameters.Sort((Comparison<FormMethodParameter>)((x, y) => x.Index.CompareTo(y.Index)));
                }
                return this.m_orderedParameters;
            }
        }

        public List<FormMethodParameter> RequiredParameters
        {
            get
            {
                if (this.m_requiredParameters == null)
                {
                    this.m_requiredParameters = new List<FormMethodParameter>();
                    foreach (string key in (IEnumerable<string>)this.ParameterCache.Keys)
                    {
                        FormMethodParameter formMethodParameter = this.ParameterCache[key];
                        if (formMethodParameter.IsRequired())
                            this.m_requiredParameters.Add(formMethodParameter);
                    }
                }
                return this.m_requiredParameters;
            }
        }

        internal void InvokeMethod(object instance, object[] values)
        {
            if (this.m_methodHandler == null)
            {
                this.m_dynamicMethod = new DynamicMethod(string.Format("___{0}", (object)this.Method.Name), this.Method.ReturnType, new Type[2]
                {
          typeof (object),
          typeof (object[])
                }, instance.GetType(), true);
                ILGenerator ilGenerator = this.m_dynamicMethod.GetILGenerator();
                if (!this.Method.IsStatic)
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
                for (int index = 0; index < this.OrderedParameters.Count; ++index)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Ldc_I4, index + 1);
                    ilGenerator.Emit(OpCodes.Ldelem_Ref);
                    Type parameterType = this.OrderedParameters[index].Parameter.ParameterType;
                    if (parameterType.IsValueType)
                        ilGenerator.Emit(OpCodes.Unbox_Any, parameterType);
                }
                if (this.Method.IsVirtual)
                    ilGenerator.Emit(OpCodes.Callvirt, this.Method);
                else
                    ilGenerator.Emit(OpCodes.Call, this.Method);
                ilGenerator.Emit(OpCodes.Ret);
                this.m_methodHandler = (FormMethodHandler)this.m_dynamicMethod.CreateDelegate(typeof(FormMethodHandler));
            }
            this.m_methodHandler(instance, values);
        }
    }
}
