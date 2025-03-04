// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal sealed record class ValidationAttribute(
    string Name,
    string ClassName,
    List<string> Arguments,
    Dictionary<string, string> NamedArguments,
    bool IsCustomValidationAttribute
);
