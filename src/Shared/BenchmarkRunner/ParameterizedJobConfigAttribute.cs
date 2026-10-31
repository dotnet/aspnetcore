// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace BenchmarkDotNet.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
internal sealed class ParameterizedJobConfigAttribute : AspNetCoreBenchmarkAttribute
{
    public ParameterizedJobConfigAttribute(Type configType) : base(configType)
    {
    }
}
