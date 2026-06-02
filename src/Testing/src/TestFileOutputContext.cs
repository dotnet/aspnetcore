// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Provides access to file storage for the running test. Get access by
/// implementing <see cref="ITestMethodLifecycle"/>, and accessing <see cref="TestContext.FileOutput"/>.
/// </summary>
/// <remarks>
/// Requires defining <see cref="AspNetTestFramework"/> as the test framework.
/// </remarks>
public sealed class TestFileOutputContext
{
    private const string InvalidFileCharsString = "\"<>|\0" +
        "\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A" +
        "\u000B\u000C\u000D\u000E\u000F\u0010\u0011\u0012\u0013\u0014" +
        "\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E" +
        "\u001F:*?\\/ \u007F";

#if NET8_0_OR_GREATER
    private static readonly SearchValues<char> InvalidFileChars = SearchValues.Create(InvalidFileCharsString);
#else
    private static readonly char[] InvalidFileChars = InvalidFileCharsString.ToCharArray();
#endif

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
        ArgumentNullThrowHelper.ThrowIfNull(prefix);

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
        if (name.StartsWith(assemblyName + ".", StringComparison.Ordinal))
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
