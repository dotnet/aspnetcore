// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

#pragma warning disable ASPNETCORE9004 // Built-in Native AOT metadata consumes the experimental descriptor model.

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal static class BuiltInComponentDescriptors
{
    internal static ComponentDescriptor[] GetDescriptors()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(Forms.DataAnnotationsValidator),
                CreateInstance = static _ => new Forms.DataAnnotationsValidator(),
                Parameters =
                [
                    new ComponentParameterDescriptor
                    {
                        Name = "CurrentEditContext",
                        ParameterType = typeof(Forms.EditContext),
                        Attribute = new CascadingParameterAttribute(),
                        GetValue = static target => GetCurrentEditContext((Forms.DataAnnotationsValidator)target),
                        SetValue = static (target, value) =>
                            SetCurrentEditContext((Forms.DataAnnotationsValidator)target, (Forms.EditContext?)value),
                    },
                ],
                Injectables =
                [
                    new ComponentInjectableDescriptor
                    {
                        Name = "ServiceProvider",
                        ServiceType = typeof(IServiceProvider),
                        Attribute = new InjectAttribute(),
                        SetValue = static (target, value) =>
                            SetServiceProvider((Forms.DataAnnotationsValidator)target, (IServiceProvider)value!),
                    },
                ],
            },
        ];

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CurrentEditContext")]
    private static extern Forms.EditContext? GetCurrentEditContext(Forms.DataAnnotationsValidator target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CurrentEditContext")]
    private static extern void SetCurrentEditContext(Forms.DataAnnotationsValidator target, Forms.EditContext? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ServiceProvider")]
    private static extern void SetServiceProvider(Forms.DataAnnotationsValidator target, IServiceProvider value);
}

#pragma warning restore ASPNETCORE9004
