using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Compression;
using Redbox.HAL.Component.Model.Extensions;
using System;
using System.Reflection;

namespace Redbox.HAL.IPC.Framework
{
    internal class FormMethodParameter
    {
        public bool IsRequired()
        {
            return !this.Parameter.ParameterType.IsValueType ? this.MetaData != null && this.MetaData.IsRequired : this.MetaData != null && this.MetaData.IsRequired;
        }

        public object ConvertValue(string value)
        {
            if (this.Parameter.ParameterType != typeof(byte[]))
                return ConversionHelper.ChangeType((object)value, this.Parameter.ParameterType);
            byte[] source;
            switch (this.MetaData.BinaryEncoding)
            {
                case BinaryEncoding.Hex:
                    source = StringExtensions.HexToBytes(value);
                    break;
                case BinaryEncoding.Base64:
                    source = StringExtensions.Base64ToBytes(value);
                    break;
                case BinaryEncoding.Ascii95:
                    throw new NotImplementedException("Ascii95 support is not implemented.");
                default:
                    throw new ArgumentException("The named paramete accepts binary data and the command form method doesn't specify a valid BinaryEncoding.");
            }
            if (source != null && this.MetaData.CompressionType != CompressionType.None)
                source = CompressionAlgorithmFactory.GetAlgorithm(this.MetaData.CompressionType).Decompress(source);
            return (object)source;
        }

        public int Index { get; set; }

        public string KeyName { get; set; }

        public ParameterInfo Parameter { get; set; }

        public CommandKeyValueAttribute MetaData { get; set; }
    }
}
