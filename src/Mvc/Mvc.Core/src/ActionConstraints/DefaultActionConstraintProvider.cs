// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// A default implementation of <see cref="IActionConstraintProvider"/>.
/// </summary>
/// <remarks>
/// This provider is able to provide an <see cref="IActionConstraint"/> instance when the
/// <see cref="IActionConstraintMetadata"/> implements <see cref="IActionConstraint"/> or
/// <see cref="IActionConstraintFactory"/>/
/// </remarks>
internal sealed class DefaultActionConstraintProvider : IActionConstraintProvider
{
    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public void OnProvidersExecuting(ActionConstraintProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        for (var i = 0; i < context.Results.Count; i++)
        {
            ProvideConstraint(context.Results[i], context.HttpContext.RequestServices);
        }
    }

    /// <inheritdoc />
    public void OnProvidersExecuted(ActionConstraintProviderContext context)
    {
    }

    private static void ProvideConstraint(ActionConstraintItem item, IServiceProvider services)
    {
        // Don't overwrite anything that was done by a previous provider.
        if (item.Constraint != null)
        {
            return;
        }

        if (item.Metadata is IActionConstraint constraint)
        {
            item.Constraint = constraint;
            item.IsReusable = true;
            return;
        }

        if (item.Metadata is IActionConstraintFactory factory)
        {
            item.Constraint = factory.CreateInstance(services);
            item.IsReusable = factory.IsReusable;
            return;
        }
    }
}
