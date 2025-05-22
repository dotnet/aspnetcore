// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Validation.ValidationsGenerator;

// This class forwards to the new generator implementation
namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    private static readonly Microsoft.Extensions.Validation.ValidationsGenerator.ValidationsGenerator _forwardingGenerator = 
        new Microsoft.Extensions.Validation.ValidationsGenerator.ValidationsGenerator();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        _forwardingGenerator.Initialize(context);
    }
}