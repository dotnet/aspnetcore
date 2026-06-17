// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Google.Protobuf.Reflection;
using Google.Rpc;

namespace Grpc.Shared;

internal sealed class DescriptorRegistry
{
    private readonly object _lock = new object();
    private readonly HashSet<FileDescriptor> _fileDescriptors = new HashSet<FileDescriptor>();
    private readonly ConcurrentDictionary<Type, DescriptorBase> _typeDescriptorMap = new ConcurrentDictionary<Type, DescriptorBase>();

    public DescriptorRegistry()
    {
        // For Grpc.Rpc.Status, which is used to send error responses.
        AddFileDescriptorsRecursive(StatusReflection.Descriptor);
    }

    public void RegisterFileDescriptor(FileDescriptor fileDescriptor)
    {
        lock (_lock)
        {
            AddFileDescriptorsRecursive(fileDescriptor);
        }
    }

    private void AddFileDescriptorsRecursive(FileDescriptor fileDescriptor)
    {
        var added = _fileDescriptors.Add(fileDescriptor);

        // If a descriptor is already added then all its types and dependencies are already be present.
        // In this case, exit immediately. This guards against the possibility of cyclical dependencies between files.
        if (!added)
        {
            return;
        }

        // Non-nested enums.
        foreach (var enumDescriptor in fileDescriptor.EnumTypes)
        {
            _typeDescriptorMap[enumDescriptor.ClrType] = enumDescriptor;
        }

        // Search messages for nested enums.
        foreach (var messageDescriptor in fileDescriptor.MessageTypes)
        {
            AddDescriptorsRecursive(messageDescriptor);
        }

        // Search imported files.
        foreach (var dependencyFile in fileDescriptor.Dependencies)
        {
            AddFileDescriptorsRecursive(dependencyFile);
        }
    }

    private void AddDescriptorsRecursive(MessageDescriptor messageDescriptor)
    {
        // Type is null for map entry message types. Just skip adding them.
        if (messageDescriptor.ClrType != null)
        {
            _typeDescriptorMap[messageDescriptor.ClrType] = messageDescriptor;
        }

        foreach (var enumDescriptor in messageDescriptor.EnumTypes)
        {
            _typeDescriptorMap[enumDescriptor.ClrType] = enumDescriptor;
        }

        foreach (var nestedMessageDescriptor in messageDescriptor.NestedTypes)
        {
            AddDescriptorsRecursive(nestedMessageDescriptor);
        }
    }

    public DescriptorBase? FindDescriptorByType(Type type)
    {
        _typeDescriptorMap.TryGetValue(type, out var value);
        return value;
    }
}
