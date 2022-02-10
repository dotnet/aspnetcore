// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal delegate Task<IActionResult?> PageHandlerExecutorDelegate(object handler, object?[]? arguments);
