// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Indicates that a type and all derived types are used to serve HTTP API responses.
/// <para>
/// Controllers decorated with this attribute are configured with features and behavior targeted at improving the
/// developer experience for building APIs.
/// </para>
/// <para>
/// When decorated on an assembly, all controllers in the assembly will be treated as controllers with API behavior.
/// For more information, see <see href="https://learn.microsoft.com/aspnet/core/web-api/#apicontroller-attribute">ApiController attribute</see>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ApiControllerAttribute : ControllerAttribute, IApiBehaviorMetadata
{
}
