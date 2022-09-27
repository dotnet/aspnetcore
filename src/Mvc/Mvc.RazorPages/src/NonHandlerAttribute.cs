// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Specifies that the targeted method is not a page handler method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class NonHandlerAttribute : Attribute
{
}
