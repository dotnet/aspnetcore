// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

internal static class ResultsOfTHelper
{
    private static readonly MethodInfo PopulateMetadataMethod = typeof(ResultsOfTHelper).GetMethod(nameof(PopulateMetadata), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static void PopulateMetadataIfTargetIsIEndpointMetadataProvider<TTarget>(EndpointMetadataContext context)
    {
        if (typeof(IEndpointMetadataProvider).IsAssignableFrom(typeof(TTarget)))
        {
            PopulateMetadataMethod.MakeGenericMethod(typeof(TTarget)).Invoke(null, new object[] { context });
        }
    }

    private static void PopulateMetadata<TTarget>(EndpointMetadataContext context) where TTarget : IEndpointMetadataProvider
    {
        TTarget.PopulateMetadata(context);
    }
}
