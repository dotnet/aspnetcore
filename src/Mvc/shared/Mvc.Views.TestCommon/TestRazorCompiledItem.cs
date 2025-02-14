// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Hosting;

public class TestRazorCompiledItem : RazorCompiledItem
{
    public static RazorCompiledItem CreateForPage(string identifier, object[] metadata = null)
    {
        return CreateForPage(type: null, identifier, metadata);
    }

    public static RazorCompiledItem CreateForPage(Type type, string identifier, object[] metadata = null)
    {
        return new TestRazorCompiledItem(type, "mvc.1.0.razor-page", identifier, metadata);
    }

    public static RazorCompiledItem CreateForView(string identifier, object[] metadata = null)
    {
        return CreateForView(type: null, identifier, metadata);
    }

    public static RazorCompiledItem CreateForView(Type type, string identifier, object[] metadata = null)
    {
        return new TestRazorCompiledItem(type, "mvc.1.0.razor-page", identifier, metadata);
    }

    public TestRazorCompiledItem(Type type, string kind, string identifier, object[] metadata)
    {
        Type = type;
        Kind = kind;
        Identifier = identifier;
        Metadata = metadata ?? Array.Empty<object>();
    }

    public override string Identifier { get; }

    public override string Kind { get; }

    public override IReadOnlyList<object> Metadata { get; }

    public override Type Type { get; }

    public static string GetChecksum(string content)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    public static string GetChecksumSHA256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
