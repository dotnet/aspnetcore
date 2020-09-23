// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Provides access to file storage for the running test. Get access by
    /// implementing <see cref="ITestMethodLifecycle"/>, and accessing <see cref="TestContext.FileOutput"/>.
    /// </summary>
    /// <remarks>
    /// Requires defining <see cref="AspNetTestFramework"/> as the test framework.
    /// </remarks>
    public sealed class TestFileOutputContext
    {
        private static char[] InvalidFileChars = new char[]
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '*', '?', '\\', '/', ' ', (char)127
        };

        private readonly TestContext _parent;

        public TestFileOutputContext(TestContext parent)
        {
            _parent = parent;

            TestName = GetTestMethodName(parent.TestMethod, parent.MethodArguments);
            TestClassName = GetTestClassName(parent.TestClass);

            AssemblyOutputDirectory = GetAssemblyBaseDirectory(_parent.TestClass.Assembly);
            if (!string.IsNullOrEmpty(AssemblyOutputDirectory))
            {
                TestClassOutputDirectory = Path.Combine(AssemblyOutputDirectory, TestClassName);
            }
        }

        public string TestName { get; }

        public string TestClassName { get; }

        public string AssemblyOutputDirectory { get; }

        public string TestClassOutputDirectory { get; }

        public string GetUniqueFileName(string prefix, string extension)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (extension != null && !extension.StartsWith(".", StringComparison.Ordinal))
            {
                throw new ArgumentException("The extension must start with '.' if one is provided.", nameof(extension));
            }

            var path = Path.Combine(TestClassOutputDirectory, $"{prefix}{extension}");

            var i = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(TestClassOutputDirectory, $"{prefix}{i++}{extension}");
            }

            return path;
        }

        // Gets the output directory without appending the TFM or assembly name.
        public static string GetOutputDirectory(Assembly assembly)
        {
            var attribute = assembly.GetCustomAttributes().OfType<TestOutputDirectoryAttribute>().FirstOrDefault();
            return attribute?.BaseDirectory;
        }

        public static string GetAssemblyBaseDirectory(Assembly assembly, string baseDirectory = null)
        {
            var attribute = assembly.GetCustomAttributes().OfType<TestOutputDirectoryAttribute>().FirstOrDefault();
            baseDirectory = baseDirectory ?? attribute?.BaseDirectory;
            if (string.IsNullOrEmpty(baseDirectory))
            {
                return string.Empty;
            }

            return Path.Combine(baseDirectory, assembly.GetName().Name, attribute.TargetFramework);
        }

        public static bool GetPreserveExistingLogsInOutput(Assembly assembly)
        {
            var attribute = assembly.GetCustomAttributes().OfType<TestOutputDirectoryAttribute>().FirstOrDefault();
            return attribute.PreserveExistingLogsInOutput;
        }

        public static string GetTestClassName(Type type)
        {
            var shortNameAttribute =
                type.GetCustomAttribute<ShortClassNameAttribute>() ??
                type.Assembly.GetCustomAttribute<ShortClassNameAttribute>();
            var name = shortNameAttribute == null ? type.FullName : type.Name;

            // Try to shorten the class name using the assembly name
            var assemblyName = type.Assembly.GetName().Name;
            if (name.StartsWith(assemblyName + "."))
            {
                name = name.Substring(assemblyName.Length + 1);
            }

            return name;
        }

        public static string GetTestMethodName(MethodInfo method, object[] arguments)
        {
            var name = arguments.Aggregate(method.Name, (a, b) => $"{a}-{(b ?? "null")}");
            return RemoveIllegalFileChars(name);
        }

        public static string RemoveIllegalFileChars(string s)
        {
            var sb = new StringBuilder();

            foreach (var c in s)
            {
                if (InvalidFileChars.Contains(c))
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    {
                        sb.Append('_');
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
