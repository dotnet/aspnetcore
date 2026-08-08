// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Media;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

#pragma warning disable ASPNETCORE9004 // Built-in Native AOT metadata consumes the experimental descriptor model.

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal static class BuiltInComponentDescriptors
{
    internal static ComponentDescriptor[] GetDescriptors()
        =>
        [
            CreateDescriptor<Image>(static _ => new Image { Source = null! }),
            CreateDescriptor<Video>(static _ => new Video { Source = null! }),
            CreateDescriptor<FileDownload>(static _ => new FileDownload { Source = null! }),
        ];

    private static ComponentDescriptor CreateDescriptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        Func<IServiceProvider, TComponent> createInstance)
        where TComponent : MediaComponentBase
        => new()
        {
            Type = typeof(TComponent),
            CreateInstance = createInstance,
            Injectables =
            [
                new ComponentInjectableDescriptor
                {
                    Name = "JSRuntime",
                    ServiceType = typeof(IJSRuntime),
                    Attribute = new InjectAttribute(),
                    SetValue = static (target, value) =>
                        MediaComponentAccessors.SetJSRuntime((MediaComponentBase)target, (IJSRuntime)value!),
                },
                new ComponentInjectableDescriptor
                {
                    Name = "LoggerFactory",
                    ServiceType = typeof(ILoggerFactory),
                    Attribute = new InjectAttribute(),
                    SetValue = static (target, value) =>
                        MediaComponentAccessors.SetLoggerFactory((MediaComponentBase)target, (ILoggerFactory)value!),
                },
            ],
        };

    private static class MediaComponentAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_JSRuntime")]
        internal static extern void SetJSRuntime(MediaComponentBase target, IJSRuntime value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_LoggerFactory")]
        internal static extern void SetLoggerFactory(MediaComponentBase target, ILoggerFactory value);
    }
}

#pragma warning restore ASPNETCORE9004
