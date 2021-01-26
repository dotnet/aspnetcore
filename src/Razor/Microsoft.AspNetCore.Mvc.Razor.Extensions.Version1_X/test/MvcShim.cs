// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
