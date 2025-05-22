// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forward validation-related types from Microsoft.Extensions.Validation
// to maintain backward compatibility

using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.IValidatableInfo))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.IValidatableInfoResolver))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.ValidatableParameterInfo))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.ValidatablePropertyInfo))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.ValidatableTypeAttribute))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.ValidatableTypeInfo))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.ValidateContext))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.Validation.ValidationOptions))]
[assembly: TypeForwardedTo(typeof(Microsoft.Extensions.DependencyInjection.ValidationServiceCollectionExtensions))]