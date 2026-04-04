#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Grpc.Shared.Server;

internal sealed class InterceptorActivators
{
    private readonly ConcurrentDictionary<Type, IGrpcInterceptorActivator> _cachedActivators = new();
    private readonly IServiceProvider _serviceProvider;

    public InterceptorActivators(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IGrpcInterceptorActivator GetInterceptorActivator(Type type)
    {
        return _cachedActivators.GetOrAdd<IServiceProvider>(type, CreateActivator, _serviceProvider);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
        Justification = "Type parameter members are preserved with DynamicallyAccessedMembers on InterceptorRegistration.Type property.")]
    [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "Type definition is explicitly specified and type argument is always an Interceptor type.")]
    private static IGrpcInterceptorActivator CreateActivator(Type type, IServiceProvider serviceProvider)
    {
        return (IGrpcInterceptorActivator)serviceProvider.GetRequiredService(typeof(IGrpcInterceptorActivator<>).MakeGenericType(type));
    }
}
