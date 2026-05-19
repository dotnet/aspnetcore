// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf.Reflection;
using Grpc.Shared;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;

internal sealed class CallHandlerDescriptorInfo
{
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
        PathDescriptorsCache = new ConcurrentDictionary<string, List<FieldDescriptor>?>();
    }

    public FieldDescriptor? ResponseBodyDescriptor { get; }
    public MessageDescriptor? BodyDescriptor { get; }
    [MemberNotNullWhen(true, nameof(BodyFieldDescriptor))]
    public bool BodyDescriptorRepeated { get; }
    public FieldDescriptor? BodyFieldDescriptor { get; }
    public Dictionary<string, RouteParameter> RouteParameterDescriptors { get; }
    public JsonTranscodingRouteAdapter RouteAdapter { get; }
    public ConcurrentDictionary<string, List<FieldDescriptor>?> PathDescriptorsCache { get; }
}
