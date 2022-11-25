// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Reflection;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal sealed class DescriptorRegistry
{
    private readonly HashSet<FileDescriptor> _fileDescriptors = new HashSet<FileDescriptor>();
    private readonly HashSet<EnumDescriptor> _enumDescriptors = new HashSet<EnumDescriptor>();

    public void RegisterFileDescriptor(FileDescriptor fileDescriptor)
    {
        AddFileDescriptorsRecursive(fileDescriptor);
    }

    private void AddFileDescriptorsRecursive(FileDescriptor fileDescriptor)
    {
        var added = _fileDescriptors.Add(fileDescriptor);

        // If a descriptor is already added then all its types and dependencies must have already be present.
        // This guards against the possibility of cyclical dependencies.
        if (!added)
        {
            return;
        }

        foreach (var descriptor in fileDescriptor.EnumTypes)
        {
            _enumDescriptors.Add(descriptor);
        }

        foreach (var messageDescriptor in fileDescriptor.MessageTypes)
        {
            AddNestedEnumDescriptorsRecursive(messageDescriptor);
        }

        foreach (var dependencyFile in fileDescriptor.Dependencies)
        {
            AddFileDescriptorsRecursive(dependencyFile);
        }
    }

    private void AddNestedEnumDescriptorsRecursive(MessageDescriptor messageDescriptor)
    {
        foreach (var enumDescriptor in messageDescriptor.EnumTypes)
        {
            _enumDescriptors.Add(enumDescriptor);
        }

        foreach (var nestedMessageDescriptor in messageDescriptor.NestedTypes)
        {
            AddNestedEnumDescriptorsRecursive(nestedMessageDescriptor);
        }
    }

    public IEnumerable<EnumDescriptor> GetEnumDescriptors()
    {
        return _enumDescriptors;
    }
}
