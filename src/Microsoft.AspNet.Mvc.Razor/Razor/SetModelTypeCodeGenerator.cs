// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Web.Razor.Generator;

namespace Microsoft.AspNet.Mvc.Razor
{
    internal class SetModelTypeCodeGenerator : SetBaseTypeCodeGenerator
    {
        private const string GenericTypeFormatString = "{0}<{1}>";

        public SetModelTypeCodeGenerator(string modelType)
            : base(modelType)
        {
        }

        protected override string ResolveType(CodeGeneratorContext context, string baseType)
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                GenericTypeFormatString,
                context.Host.DefaultBaseClass,
                baseType);
        }
    }
}
