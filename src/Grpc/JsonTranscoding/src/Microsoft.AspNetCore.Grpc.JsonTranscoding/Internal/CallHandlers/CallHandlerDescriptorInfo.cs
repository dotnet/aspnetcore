// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Google.Protobuf.Reflection;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;

internal sealed class CallHandlerDescriptorInfo
{
    public CallHandlerDescriptorInfo(
        FieldDescriptor? responseBodyDescriptor,
        MessageDescriptor? bodyDescriptor,
        bool bodyDescriptorRepeated,
        List<FieldDescriptor>? bodyFieldDescriptors,
        Dictionary<string, List<FieldDescriptor>> routeParameterDescriptors,
        JsonTranscodingRouteAdapter routeAdapter)
    {
        ResponseBodyDescriptor = responseBodyDescriptor;
        BodyDescriptor = bodyDescriptor;
        BodyDescriptorRepeated = bodyDescriptorRepeated;
        BodyFieldDescriptors = bodyFieldDescriptors;
        RouteParameterDescriptors = routeParameterDescriptors;
        RouteAdapter = routeAdapter;
        if (BodyFieldDescriptors != null)
        {
            BodyFieldDescriptorsPath = string.Join('.', BodyFieldDescriptors.Select(d => d.Name));
        }
        PathDescriptorsCache = new ConcurrentDictionary<string, List<FieldDescriptor>?>();
    }

    public FieldDescriptor? ResponseBodyDescriptor { get; }
    public MessageDescriptor? BodyDescriptor { get; }
    [MemberNotNullWhen(true, nameof(BodyFieldDescriptors), nameof(BodyFieldDescriptorsPath))]
    public bool BodyDescriptorRepeated { get; }
    public List<FieldDescriptor>? BodyFieldDescriptors { get; }
    public Dictionary<string, List<FieldDescriptor>> RouteParameterDescriptors { get; }
    public JsonTranscodingRouteAdapter RouteAdapter { get; }
    public ConcurrentDictionary<string, List<FieldDescriptor>?> PathDescriptorsCache { get; }
    public string? BodyFieldDescriptorsPath { get; }
}
