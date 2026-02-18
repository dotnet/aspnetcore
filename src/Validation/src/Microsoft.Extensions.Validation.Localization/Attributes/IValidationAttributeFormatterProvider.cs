// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization.Attributes;

public interface IValidationAttributeFormatterProvider
{
    public IValidationAttributeFormatter GetFormatter(ValidationAttribute attribute);
}
