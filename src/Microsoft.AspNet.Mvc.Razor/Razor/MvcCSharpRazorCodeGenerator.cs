#if NET45
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.CodeDom;
using System.Web.Razor;
using System.Web.Razor.Generator;

namespace Microsoft.AspNet.Mvc.Razor
{
    internal class MvcCSharpRazorCodeGenerator : CSharpRazorCodeGenerator
    {
        private const string DefaultModelTypeName = "dynamic";

        public MvcCSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
            
            // set the default model type to "dynamic" (Dev10 bug 935656)
            // don't set it for "special" pages (such as "_viewStart.cshtml")
            SetBaseType(DefaultModelTypeName);
        }

        private void SetBaseType(string modelTypeName)
        {
            var baseType = new CodeTypeReference(Context.Host.DefaultBaseClass + "<" + modelTypeName + ">");
            Context.GeneratedClass.BaseTypes.Clear();
            Context.GeneratedClass.BaseTypes.Add(baseType);
        }
    }
}
#endif