// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;
using Xunit;

#pragma warning disable ASP0029

namespace Microsoft.AspNetCore.Components.Forms;

public class MevReproTest
{
    [Fact]
    public async Task MevPath_PerFieldAsyncAttribute_ProducesMessage()
    {
        var sp = BuildProvider();
        var model = new MevModel { Username = "taken" };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(sp);

        var isValid = await editContext.ValidateAsync();
        var messages = editContext.GetValidationMessages().ToArray();

        Assert.Equal(new[] { "Username is taken" }, messages);
        Assert.False(isValid);
    }

    [Fact]
    public async Task MevPath_FormLevelAsyncObject_ProducesMessage()
    {
        var sp = BuildProvider();
        var model = new MevModel { Username = "reserved" };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(sp);

        var isValid = await editContext.ValidateAsync();
        var messages = editContext.GetValidationMessages().ToArray();

        Assert.Equal(new[] { "Username is reserved" }, messages);
        Assert.False(isValid);
    }

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddValidation(options => options.Resolvers.Add(new Resolver()));
        return services.BuildServiceProvider();
    }

    private sealed class AsyncAvailabilityAttribute : AsyncValidationAttribute
    {
        protected override async Task<ValidationResult> IsValidAsync(object value, ValidationContext validationContext, CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (string.Equals(value as string, "taken", System.StringComparison.Ordinal))
            {
                return new ValidationResult("Username is taken", new[] { nameof(MevModel.Username) });
            }
            return ValidationResult.Success;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            => throw new System.NotSupportedException();
    }

    private sealed class MevModel : BaseModel
    {
    }

    private abstract class BaseModel : IAsyncValidatableObject
    {
        [Required(ErrorMessage = "Username is required.")]
        [AsyncAvailability]
        public string Username { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            => Enumerable.Empty<ValidationResult>();

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (string.Equals(Username, "reserved", System.StringComparison.Ordinal))
            {
                yield return new ValidationResult("Username is reserved", new[] { nameof(Username) });
            }
        }
    }

    private sealed class Resolver : IValidatableInfoResolver
    {
        public bool TryGetValidatableTypeInfo(System.Type type, [NotNullWhen(true)] out IValidatableTypeInfo validatableTypeInfo)
        {
            if (type == typeof(MevModel))
            {
                validatableTypeInfo = new ModelTypeInfo(typeof(MevModel),
                [
                    new ModelPropertyInfo(
                        typeof(MevModel),
                        typeof(string),
                        nameof(MevModel.Username),
                        [new RequiredAttribute { ErrorMessage = "Username is required." }, new AsyncAvailabilityAttribute()]),
                ]);
                return true;
            }

            validatableTypeInfo = null;
            return false;
        }

        public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo validatableParameterInfo)
        {
            validatableParameterInfo = null;
            return false;
        }

        private sealed class ModelTypeInfo : ValidatableTypeInfo
        {
            public ModelTypeInfo(System.Type type, IReadOnlyList<ValidatablePropertyInfo> members) : base(type, members) { }
            protected override ValidationAttribute[] GetValidationAttributes() => [];
        }

        private sealed class ModelPropertyInfo : ValidatablePropertyInfo
        {
            private readonly ValidationAttribute[] _attributes;
            public ModelPropertyInfo(System.Type declaringType, System.Type propertyType, string name, ValidationAttribute[] attributes)
                : base(declaringType, propertyType, name, displayNameInfo: null)
            {
                _attributes = attributes;
            }
            protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
        }
    }
}
