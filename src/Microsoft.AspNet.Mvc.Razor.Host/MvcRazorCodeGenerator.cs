// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.CodeDom;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator;

namespace Microsoft.AspNet.Mvc.Razor
{
    internal class MvcRazorCodeGenerator : CSharpRazorCodeGenerator
    {
        private const string DefaultModelTypeName = "dynamic";

        public MvcRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
            var mvcHost = host as MvcRazorHost;
            if (mvcHost != null)
            {
                // set the default model type to "dynamic" (Dev10 bug 935656)
                // don't set it for "special" pages (such as "_viewStart.cshtml")
                SetBaseType(DefaultModelTypeName);
            }
        }

        private void SetBaseType(string modelTypeName)
        {
            var baseType = new CodeTypeReference(Context.Host.DefaultBaseClass + "<" + modelTypeName + ">");
            Context.GeneratedClass.BaseTypes.Clear();
            Context.GeneratedClass.BaseTypes.Add(baseType);
        }
    }
}
