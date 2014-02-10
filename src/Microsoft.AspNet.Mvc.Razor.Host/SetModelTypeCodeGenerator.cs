using System;
using System.Globalization;
using Microsoft.AspNet.Razor.Generator;

namespace Microsoft.AspNet.Mvc.Razor
{
    internal class SetModelTypeCodeGenerator : SetBaseTypeCodeGenerator
    {
        private string _genericTypeFormat;

        public SetModelTypeCodeGenerator(string modelType, string genericTypeFormat)
            : base(modelType)
        {
            _genericTypeFormat = genericTypeFormat;
        }

        protected override string ResolveType(CodeGeneratorContext context, string baseType)
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                _genericTypeFormat,
                context.Host.DefaultBaseClass,
                baseType);
        }

        public override string ToString()
        {
            return "Model:" + BaseType;
        }
    }
}
