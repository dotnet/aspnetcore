// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// A base class for components that display differing content depending on the user's authorization status.
/// </summary>
[CacheBoundaryLiveComponent(Disallow = true, VaryBy = CacheBoundaryVaryBy.User)]
public abstract class AuthorizeViewCore : ComponentBase
{
    private AuthenticationState? currentAuthenticationState;
    private bool? isAuthorized;

    /// <summary>
    /// The content that will be displayed if the user is authorized.
    /// </summary>
    [Parameter] public RenderFragment<AuthenticationState>? ChildContent { get; set; }

    /// <summary>
    /// The content that will be displayed if the user is not authorized.
    /// </summary>
    [Parameter] public RenderFragment<AuthenticationState>? NotAuthorized { get; set; }

    /// <summary>
    /// The content that will be displayed if the user is authorized.
    /// If you specify a value for this parameter, do not also specify a value for <see cref="ChildContent"/>.
    /// </summary>
    [Parameter] public RenderFragment<AuthenticationState>? Authorized { get; set; }

    /// <summary>
    /// The content that will be displayed while asynchronous authorization is in progress.
    /// </summary>
    [Parameter] public RenderFragment? Authorizing { get; set; }

    /// <summary>
    /// The resource to which access is being controlled.
    /// </summary>
    [Parameter] public object? Resource { get; set; }

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Inject] private IAuthorizationPolicyProvider AuthorizationPolicyProvider { get; set; } = default!;

    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // We're using the same sequence number for each of the content items here
        // so that we can update existing instances if they are the same shape
        if (isAuthorized == null)
        {
            builder.AddContent(0, Authorizing);
        }
        else if (isAuthorized == true)
        {
            var authorized = Authorized ?? ChildContent;
            builder.AddContent(0, authorized?.Invoke(currentAuthenticationState!));
        }
        else
        {
            builder.AddContent(0, NotAuthorized?.Invoke(currentAuthenticationState!));
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // We allow 'ChildContent' for convenience in basic cases, and 'Authorized' for symmetry
        // with 'NotAuthorized' in other cases. Besides naming, they are equivalent. To avoid
        // confusion, explicitly prevent the case where both are supplied.
        if (ChildContent != null && Authorized != null)
        {
            throw new InvalidOperationException($"Do not specify both '{nameof(Authorized)}' and '{nameof(ChildContent)}'.");
        }

        if (AuthenticationState == null)
        {
            throw new InvalidOperationException($"Authorization requires a cascading parameter of type Task<{nameof(AuthenticationState)}>. Consider using {typeof(CascadingAuthenticationState).Name} to supply this.");
        }

        // Clear the previous result of authorization
        // This will cause the Authorizing state to be displayed until the authorization has been completed
        isAuthorized = null;

        currentAuthenticationState = await AuthenticationState;
        isAuthorized = await IsAuthorizedAsync(currentAuthenticationState.User);
    }

    /// <summary>
    /// Gets the data required to apply authorization rules.
    /// </summary>
    protected abstract IAuthorizeData[]? GetAuthorizeData();

    /// <summary>
    /// Gets the <see cref="IAuthorizationRequirementData"/> used to apply authorization rules, if any.
    /// </summary>
    /// <remarks>
    /// By default this returns <see langword="null"/>. Override this method to contribute
    /// <see cref="IAuthorizationRequirementData"/> in addition to the <see cref="IAuthorizeData"/>
    /// returned by <see cref="GetAuthorizeData"/>.
    /// </remarks>
    /// <returns>
    /// The <see cref="IAuthorizationRequirementData"/> used to build the effective policy, or
    /// <see langword="null"/> if none applies.
    /// </returns>
    protected virtual IAuthorizationRequirementData[]? GetAuthorizationRequirementData() => null;

    private async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user)
    {
        var authorizeData = GetAuthorizeData();
        var requirementData = GetAuthorizationRequirementData();
        if (authorizeData is null && requirementData is null)
        {
            // No authorization applies, so no need to consult the authorization service
            return true;
        }

        EnsureNoAuthenticationSchemeSpecified(authorizeData);

        var metadata = CombineAuthorizationMetadata(authorizeData, requirementData);
        var policy = await AuthorizationPolicy.CombineAsync(
            AuthorizationPolicyProvider, metadata);
        var result = await AuthorizationService.AuthorizeAsync(user, Resource, policy!);
        return result.Succeeded;
    }

    private static object[] CombineAuthorizationMetadata(IAuthorizeData[]? authorizeData, IAuthorizationRequirementData[]? requirementData)
    {
        // Arrays are covariant, so when only one kind of metadata is present the typed array can be
        // handed to CombineAsync directly as object[].
        if (requirementData is null || requirementData.Length == 0)
        {
            return authorizeData ?? Array.Empty<object>();
        }

        if (authorizeData is null || authorizeData.Length == 0)
        {
            return requirementData;
        }

        // Combine both sources, but ensure an attribute that implements both IAuthorizeData and
        // IAuthorizationRequirementData is only included once so it isn't processed twice.
        var combined = new List<object>(authorizeData.Length + requirementData.Length);
        combined.AddRange(authorizeData);
        foreach (var data in requirementData)
        {
            if (!ContainsReference(authorizeData, data))
            {
                combined.Add(data);
            }
        }

        return combined.ToArray();
    }

    private static bool ContainsReference(IAuthorizeData[] authorizeData, object data)
    {
        foreach (var entry in authorizeData)
        {
            if (ReferenceEquals(entry, data))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureNoAuthenticationSchemeSpecified(IAuthorizeData[]? authorizeData)
    {
        if (authorizeData is null)
        {
            return;
        }

        // It's not meaningful to specify a nonempty scheme, since by the time Components
        // authorization runs, we already have a specific ClaimsPrincipal (we're stateful).
        // To avoid any confusion, ensure the developer isn't trying to specify a scheme.
        for (var i = 0; i < authorizeData.Length; i++)
        {
            var entry = authorizeData[i];
            if (!string.IsNullOrEmpty(entry.AuthenticationSchemes))
            {
                throw new NotSupportedException($"The authorization data specifies an authentication scheme with value '{entry.AuthenticationSchemes}'. Authentication schemes cannot be specified for components.");
            }
        }
    }
}
