// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc;

public class TestApplicationPart : ApplicationPart, IApplicationPartTypeProvider
{
    public TestApplicationPart()
    {
        Types = Enumerable.Empty<TypeInfo>();
    }

    public TestApplicationPart(params TypeInfo[] types)
    {
        Types = types;
    }

    public TestApplicationPart(IEnumerable<TypeInfo> types)
    {
        Types = types;
    }

    public TestApplicationPart(IEnumerable<Type> types)
        : this(types.Select(t => t.GetTypeInfo()))
    {
    }

    public TestApplicationPart(params Type[] types)
        : this(types.Select(t => t.GetTypeInfo()))
    {
    }

    public override string Name => "Test part";

    public IEnumerable<TypeInfo> Types { get; }
}
