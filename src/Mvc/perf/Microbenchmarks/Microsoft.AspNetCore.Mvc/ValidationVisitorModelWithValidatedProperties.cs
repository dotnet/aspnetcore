// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class ValidationVisitorModelWithValidatedProperties : ValidationVisitorBenchmarkBase
{
    public class Person
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Name { get; set; }

        public string Description { get; set; }

        public IList<Address> Address { get; set; }
    }

    public class Address
    {
        [Required]
        public string Street { get; set; }

        public string Street2 { get; set; }

        public string Type { get; set; }

        [Required]
        public string Zip { get; set; }
    }

    public override object Model { get; } = new Person
    {
        Id = 10,
        Name = "Test",
        Address = new List<Address>
            {
                new Address
                {
                    Street = "1 Microsoft Way",
                    Type = "Work",
                    Zip = "98056",
                },
                new Address
                {
                    Street = "15701 NE 39th St",
                    Type = "Home",
                    Zip = "98052",
                }
            },
    };

    [Benchmark(Baseline = true, Description = "validation for a model with some validated properties - baseline", OperationsPerInvoke = Iterations)]
    public void Visit_TypeWithSomeValidatedProperties_Baseline()
    {
        // Baseline for validating a typical model with some properties that require validation.
        // This executes without the ModelMetadata.HasValidators optimization.

        var validationVisitor = new ValidationVisitor(
            ActionContext,
            CompositeModelValidatorProvider,
            ValidatorCache,
            BaselineModelMetadataProvider,
            new ValidationStateDictionary());

        validationVisitor.Validate(BaselineModelMetadata, "key", Model);
    }

    [Benchmark(Description = "validation for a model with some validated properties", OperationsPerInvoke = Iterations)]
    public void Visit_TypeWithSomeValidatedProperties()
    {
        // Validating a typical model with some properties that require validation.
        // This executes with the ModelMetadata.HasValidators optimization.
        var validationVisitor = new ValidationVisitor(
            ActionContext,
            CompositeModelValidatorProvider,
            ValidatorCache,
            ModelMetadataProvider,
            new ValidationStateDictionary());

        validationVisitor.Validate(ModelMetadata, "key", Model);
    }
}
