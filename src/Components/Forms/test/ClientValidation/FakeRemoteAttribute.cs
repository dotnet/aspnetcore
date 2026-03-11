// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Simulates RemoteAttributeBase for testing the Blazor guard in DefaultClientValidationService.
/// The real RemoteAttributeBase lives in Microsoft.AspNetCore.Mvc.ViewFeatures, which
/// Components.Forms must not reference. This test double has the same FullName so
/// the type-name-based detection in ThrowIfRemoteAttribute matches it.
/// </summary>
internal class RemoteAttributeBase : ValidationAttribute
{
    public override bool IsValid(object? value) => true;
}

/// <summary>
/// Simulates RemoteAttribute (which inherits RemoteAttributeBase in MVC).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class FakeRemoteAttribute : RemoteAttributeBase;
