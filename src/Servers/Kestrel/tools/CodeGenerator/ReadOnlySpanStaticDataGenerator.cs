// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace CodeGenerator;

public static class ReadOnlySpanStaticDataGenerator
{
    public static string GenerateFile(string namespaceName, string className, IEnumerable<(string Name, string Value)> allProperties)
    {
        var properties = allProperties.Select((p, index) => new Property
        {
            Data = p,
            Index = index
        });

        var s = $@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace {namespaceName}
{{
    internal partial class {className}
    {{
        // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
     {Each(properties, p => $@"
        private static ReadOnlySpan<byte> {p.Data.Name}Bytes => ""{p.Data.Value}""u8;")}
    }}
}}
";

        return s;
    }

    private static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
    {
        return values.Any() ? values.Select(formatter).Aggregate((a, b) => a + b) : "";
    }

    private sealed class Property
    {
        public (string Name, string Value) Data;
        public int Index;
    }
}
