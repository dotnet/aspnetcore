// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Represents a group of related APIs.
/// </summary>
/// <remarks>
/// Endpoints are grouped by their <see cref="ApiDescription.GroupName"/>, which can be set
/// using the <see cref="Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithGroupName{TBuilder}(TBuilder, string)">WithGroupName</see> extension method on minimal API endpoints or the
/// <c>[ApiExplorerSettings(GroupName = "...")]</c> attribute on controller actions. Endpoints
/// without an explicit group name are placed in a single group with a <see langword="null"/>
/// <see cref="GroupName"/>.
/// <para>
/// Note that <see cref="Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGroup(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string)">MapGroup</see> does not set the group name for its endpoints. It only applies
/// a route prefix. To assign a group name to all endpoints in a route group, chain
/// <see cref="Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithGroupName{TBuilder}(TBuilder, string)">WithGroupName</see> on the <see cref="Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGroup(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string)">MapGroup</see> call.
/// </para>
/// </remarks>
public class ApiDescriptionGroup
{
    /// <summary>
    /// Creates a new <see cref="ApiDescriptionGroup"/>.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="items">A collection of <see cref="ApiDescription"/> items for this group.</param>
    public ApiDescriptionGroup(string? groupName, IReadOnlyList<ApiDescription> items)
    {
        GroupName = groupName;
        Items = items;
    }

    /// <summary>
    /// The group name.
    /// </summary>
    public string? GroupName { get; }

    /// <summary>
    /// A collection of <see cref="ApiDescription"/> items for this group.
    /// </summary>
    public IReadOnlyList<ApiDescription> Items { get; }
}
