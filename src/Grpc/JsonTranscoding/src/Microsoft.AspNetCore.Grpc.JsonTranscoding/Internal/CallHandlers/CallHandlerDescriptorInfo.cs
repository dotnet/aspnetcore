// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf.Reflection;
using Grpc.Shared;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;

internal sealed class CallHandlerDescriptorInfo
{
    internal readonly int MaxPathDescriptorsCacheCount;

    public CallHandlerDescriptorInfo(
        FieldDescriptor? responseBodyDescriptor,
        MessageDescriptor? bodyDescriptor,
        bool bodyDescriptorRepeated,
        FieldDescriptor? bodyFieldDescriptor,
        Dictionary<string, RouteParameter> routeParameterDescriptors,
        JsonTranscodingRouteAdapter routeAdapter)
    {
        ResponseBodyDescriptor = responseBodyDescriptor;
        BodyDescriptor = bodyDescriptor;
        BodyDescriptorRepeated = bodyDescriptorRepeated;
        BodyFieldDescriptor = bodyFieldDescriptor;
        RouteParameterDescriptors = routeParameterDescriptors;
        RouteAdapter = routeAdapter;
        PathDescriptorsCache = new ConcurrentDictionary<string, List<FieldDescriptor>>();

        MaxPathDescriptorsCacheCount = AppContext.GetData("Microsoft.AspNetCore.Grpc.JsonTranscoding.MaxPathDescriptorsCacheCount") switch
        {
            int count when count > 0 => count,
            string countStr when int.TryParse(countStr, out var parsed) && parsed > 0 => parsed,
            _ => 1000,
        };
    }

    public FieldDescriptor? ResponseBodyDescriptor { get; }
    public MessageDescriptor? BodyDescriptor { get; }
    [MemberNotNullWhen(true, nameof(BodyFieldDescriptor))]
    public bool BodyDescriptorRepeated { get; }
    public FieldDescriptor? BodyFieldDescriptor { get; }
    public Dictionary<string, RouteParameter> RouteParameterDescriptors { get; }
    public JsonTranscodingRouteAdapter RouteAdapter { get; }
    public ConcurrentDictionary<string, List<FieldDescriptor>> PathDescriptorsCache { get; }

    public bool TryAddPathDescriptors(string path, List<FieldDescriptor> pathDescriptors)
    {
        if (PathDescriptorsCache.Count >= MaxPathDescriptorsCacheCount)
        {
            return false;
        }

        return PathDescriptorsCache.TryAdd(path, pathDescriptors);
    }
}
