// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Default implementation of <see cref="IViewComponentDescriptorProvider"/>.
/// </summary>
public class DefaultViewComponentDescriptorProvider : IViewComponentDescriptorProvider
{
    private const string AsyncMethodName = "InvokeAsync";
    private const string SyncMethodName = "Invoke";
    private readonly ApplicationPartManager _partManager;

    /// <summary>
    /// Creates a new <see cref="DefaultViewComponentDescriptorProvider"/>.
    /// </summary>
    /// <param name="partManager">The <see cref="ApplicationPartManager"/>.</param>
    public DefaultViewComponentDescriptorProvider(ApplicationPartManager partManager)
    {
        ArgumentNullException.ThrowIfNull(partManager);

        _partManager = partManager;
    }

    /// <inheritdoc />
    public virtual IEnumerable<ViewComponentDescriptor> GetViewComponents()
    {
        return GetCandidateTypes().Select(CreateDescriptor);
    }

    /// <summary>
    /// Gets the candidate <see cref="TypeInfo"/> instances provided by the <see cref="ApplicationPartManager"/>.
    /// </summary>
    /// <returns>A list of <see cref="TypeInfo"/> instances.</returns>
    protected virtual IEnumerable<TypeInfo> GetCandidateTypes()
    {
        var feature = new ViewComponentFeature();
        _partManager.PopulateFeature(feature);
        return feature.ViewComponents;
    }

    private static ViewComponentDescriptor CreateDescriptor(TypeInfo typeInfo)
    {
        var methodInfo = FindMethod(typeInfo.AsType());
        var candidate = new ViewComponentDescriptor
        {
            FullName = ViewComponentConventions.GetComponentFullName(typeInfo),
            ShortName = ViewComponentConventions.GetComponentName(typeInfo),
            TypeInfo = typeInfo,
            MethodInfo = methodInfo,
            Parameters = methodInfo.GetParameters()
        };

        return candidate;
    }

    private static MethodInfo FindMethod(Type componentType)
    {
        var componentName = componentType.FullName;
        var methods = componentType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method =>
                string.Equals(method.Name, AsyncMethodName, StringComparison.Ordinal) ||
                string.Equals(method.Name, SyncMethodName, StringComparison.Ordinal))
            .ToArray();

        if (methods.Length == 0)
        {
            throw new InvalidOperationException(
                Resources.FormatViewComponent_CannotFindMethod(SyncMethodName, AsyncMethodName, componentName));
        }
        else if (methods.Length > 1)
        {
            throw new InvalidOperationException(
                Resources.FormatViewComponent_AmbiguousMethods(componentName, AsyncMethodName, SyncMethodName));
        }

        var selectedMethod = methods[0];
        if (string.Equals(selectedMethod.Name, AsyncMethodName, StringComparison.Ordinal))
        {
            if (!selectedMethod.ReturnType.IsGenericType ||
                selectedMethod.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                throw new InvalidOperationException(Resources.FormatViewComponent_AsyncMethod_ShouldReturnTask(
                    AsyncMethodName,
                    componentName,
                    nameof(Task)));
            }
        }
        else
        {
            // Will invoke synchronously. Method must not return void, Task or Task<T>.
            if (selectedMethod.ReturnType == typeof(void))
            {
                throw new InvalidOperationException(Resources.FormatViewComponent_SyncMethod_ShouldReturnValue(
                    SyncMethodName,
                    componentName));
            }
            else if (typeof(Task).IsAssignableFrom(selectedMethod.ReturnType))
            {
                throw new InvalidOperationException(Resources.FormatViewComponent_SyncMethod_CannotReturnTask(
                    SyncMethodName,
                    componentName,
                    nameof(Task)));
            }
        }

        return selectedMethod;
    }
}
