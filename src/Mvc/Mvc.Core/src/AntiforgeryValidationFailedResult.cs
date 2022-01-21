// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Core.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="BadRequestResult"/> used for antiforgery validation
/// failures. Use <see cref="IAntiforgeryValidationFailedResult"/> to
/// match for validation failures inside MVC result filters.
/// </summary>
public class AntiforgeryValidationFailedResult : BadRequestResult, IAntiforgeryValidationFailedResult
{ }
