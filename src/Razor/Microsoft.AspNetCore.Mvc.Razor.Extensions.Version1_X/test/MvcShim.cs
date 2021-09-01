// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    internal static class MvcShim
    {
        public static readonly string AssemblyName = "Microsoft.AspNetCore.Razor.Test.MvcShim.Version1_X";

        private static Assembly _assembly;
        private static CSharpCompilation _baseCompilation;

        public static Assembly Assembly
        {
            get
            {
                if (_assembly == null)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), AssemblyName + ".dll");
                    _assembly = Assembly.LoadFrom(filePath);
                }

                return _assembly;
            }
        }

        public static CSharpCompilation BaseCompilation
        {
            get
            {
                if (_baseCompilation == null)
                {
                    _baseCompilation = TestCompilation.Create(Assembly);
                }

                return _baseCompilation;
            }
        }
    }
}
