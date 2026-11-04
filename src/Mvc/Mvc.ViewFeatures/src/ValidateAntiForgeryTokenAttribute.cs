// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that the class or method that this attribute is applied validates the anti-forgery token.
/// If the anti-forgery token is not available, or if the token is invalid, the validation will fail
/// and the action method will not execute.
/// </summary>
/// <remarks>
/// This attribute helps defend against cross-site request forgery. It won't prevent other forgery or tampering
/// attacks.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ValidateAntiForgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter
{
    /// <summary>
    /// Gets the order value for determining the order of execution of filters. Filters execute in
    /// ascending numeric value of the <see cref="Order"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Filters are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
    /// </para>
    /// <para>
    /// The default Order for this attribute is 1000 because it must run after any filter which does authentication
    /// or login in order to allow them to behave as expected (ie Unauthenticated or Redirect instead of 400).
    /// </para>
    /// <para>
    /// Look at <see cref="IOrderedFilter.Order"/> for more detailed info.
    /// </para>
    /// </remarks>
    public int Order { get; set; } = 1000;

    /// <inheritdoc />
    public bool IsReusable => true;

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ValidateAntiforgeryTokenAuthorizationFilter>();
    }
}
