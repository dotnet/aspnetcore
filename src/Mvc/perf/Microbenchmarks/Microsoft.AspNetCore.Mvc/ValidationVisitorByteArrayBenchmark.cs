// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class ValidationVisitorByteArrayBenchmark : ValidationVisitorBenchmarkBase
{
    public override object Model { get; } = new byte[30];

    [Benchmark(Baseline = true, Description = "validation for byte arrays baseline", OperationsPerInvoke = Iterations)]
    public void Baseline()
    {
        // Baseline for validating a byte array of size 30, without the ModelMetadata.HasValidators optimization.
        // This is the behavior as of 2.1.
        var validationVisitor = new ValidationVisitor(
            ActionContext,
            CompositeModelValidatorProvider,
            ValidatorCache,
            BaselineModelMetadataProvider,
            new ValidationStateDictionary());

        validationVisitor.Validate(BaselineModelMetadata, "key", Model);
    }

    [Benchmark(Description = "validation for byte arrays", OperationsPerInvoke = Iterations)]
    public void HasValidators()
    {
        // Validating a byte array of size 30, with the ModelMetadata.HasValidators optimization.
        var validationVisitor = new ValidationVisitor(
            ActionContext,
            CompositeModelValidatorProvider,
            ValidatorCache,
            ModelMetadataProvider,
            new ValidationStateDictionary());

        validationVisitor.Validate(ModelMetadata, "key", Model);
    }
}
