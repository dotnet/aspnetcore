// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

internal static class ResultsOfTHelper
{
    private static readonly MethodInfo PopulateMetadataMethod = typeof(ResultsOfTHelper).GetMethod(nameof(PopulateMetadata), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static void PopulateMetadataIfTargetIsIEndpointMetadataProvider<TTarget>(MethodInfo method, EndpointBuilder builder)
    {
        if (typeof(IEndpointMetadataProvider).IsAssignableFrom(typeof(TTarget)))
        {
            PopulateMetadataMethod.MakeGenericMethod(typeof(TTarget)).Invoke(null, new object[] { method, builder });
        }
    }

    private static void PopulateMetadata<TTarget>(MethodInfo method, EndpointBuilder builder) where TTarget : IEndpointMetadataProvider
    {
        TTarget.PopulateMetadata(method, builder);
    }
}
