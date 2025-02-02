// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator
{
    internal static string EmitEndpointKey()
    {
        var writer = new StringWriter();
        var code = new CodeWriter(writer, baseIndent: 1);
        code.WriteLine("file class EndpointKey(string route, global::System.Collections.Generic.IEnumerable<string> methods)");
        code.StartBlock();
        code.WriteLine("public string Route { get; } = route;");
        code.WriteLine("public global::System.Collections.Generic.IEnumerable<string> Methods { get; } = methods;");
        code.WriteLine();
        code.WriteLine("public override bool Equals(object? obj)");
        code.StartBlock();
        code.WriteLine("if (obj is EndpointKey other)");
        code.StartBlock();
        code.WriteLine("return string.Equals(Route, other.Route, global::System.StringComparison.OrdinalIgnoreCase) &&");
        code.WriteLine("Methods.SequenceEqual(other.Methods, global::System.StringComparer.OrdinalIgnoreCase);");
        code.EndBlock();
        code.WriteLine("return false;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public override int GetHashCode()");
        code.StartBlock();
        code.WriteLine("int hash = 17;");
        code.WriteLine("hash = hash * 23 + (Route?.GetHashCode(global::System.StringComparison.OrdinalIgnoreCase) ?? 0);");
        code.WriteLine("hash = hash * 23 + GetMethodsHashCode(Methods);");
        code.WriteLine("return hash;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("private static int GetMethodsHashCode(global::System.Collections.Generic.IEnumerable<string> methods)");
        code.StartBlock();
        code.WriteLine("if (methods == null)");
        code.StartBlock();
        code.WriteLine("return 0;");
        code.EndBlock();
        code.WriteLine("int hash = 17;");
        code.WriteLine("foreach (var method in methods)");
        code.StartBlock();
        code.WriteLine("hash = hash * 23 + (method?.GetHashCode(global::System.StringComparison.OrdinalIgnoreCase) ?? 0);");
        code.EndBlock();
        code.WriteLine("return hash;");
        code.EndBlock();
        code.EndBlock();
        return writer.ToString();
    }
}
