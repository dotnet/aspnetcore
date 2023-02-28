// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class TestService
{
    public string TestServiceMethod() => "Produced from service!";
}

public class Todo
{
    public int Id { get; set; }
    public string Name { get; set; } = "Todo";
    public bool IsComplete { get; set; }
}

public class CustomFromBodyAttribute : Attribute, IFromBodyMetadata
{
    public bool AllowEmpty { get; set; }
}

public enum MyEnum { ValueA, ValueB, }

public record MyTryParseRecord(Uri Uri)
{
    public static bool TryParse(string value, out MyTryParseRecord result)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            result = null;
            return false;
        }

        result = new MyTryParseRecord(uri);
        return true;
    }
}

public struct BodyStruct
{
    public int Id { get; set; }
}

