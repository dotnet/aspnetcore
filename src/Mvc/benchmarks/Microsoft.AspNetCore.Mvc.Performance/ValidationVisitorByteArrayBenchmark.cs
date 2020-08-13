// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Performance
{
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
}
