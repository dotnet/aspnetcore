// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Rewrite.Tests.IISUrlRewrite;

public class TestServerVariablesFeature : IServerVariablesFeature
{
    private readonly Dictionary<string, string> _variables;

    public TestServerVariablesFeature(Dictionary<string, string> variables)
    {
        _variables = variables;
    }

    public string this[string variableName]
    {
        get => _variables[variableName];
        set => _variables[variableName] = value;
    }
}
