// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class RuntimeGeneratorTests : RequestDelegateGeneratorTests
{
    protected override bool IsGeneratorEnabled { get; } = false;
}
