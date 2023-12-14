// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Swashbuckle.AspNetCore.SwaggerGen;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.Swagger.Internal;

internal sealed class GrpcDataContractResolver : ISerializerDataContractResolver
{
    private readonly ISerializerDataContractResolver _innerContractResolver;
    private readonly DescriptorRegistry _descriptorRegistry;

    public GrpcDataContractResolver(ISerializerDataContractResolver innerContractResolver, DescriptorRegistry descriptorRegistry)
    {
        _innerContractResolver = innerContractResolver;
        _descriptorRegistry = descriptorRegistry;
    }

    public DataContract GetDataContractForType(Type type)
    {
        var descriptor = _descriptorRegistry.FindDescriptorByType(type);
        if (descriptor != null)
        {
            if (descriptor is MessageDescriptor messageDescriptor)
            {
                return ConvertMessage(messageDescriptor);
            }
            else if (descriptor is EnumDescriptor enumDescriptor)
            {
                return DataContract.ForPrimitive(type, DataType.String, dataFormat: null, value =>
                {
                    var match = enumDescriptor.Values.SingleOrDefault(v => v.Number == (int)value);
                    var name = match?.Name ?? value.ToString();
                    return @"""" + name + @"""";
                });
            }
        }

        return _innerContractResolver.GetDataContractForType(type);
    }

    private bool TryCustomizeMessage(MessageDescriptor messageDescriptor, [NotNullWhen(true)] out DataContract? dataContract)
    {
        // The messages serialized here should be kept in sync with ServiceDescriptionHelper.IsCustomType.
        if (ServiceDescriptorHelpers.IsWellKnownType(messageDescriptor))
        {
            if (ServiceDescriptorHelpers.IsWrapperType(messageDescriptor))
            {
                var field = messageDescriptor.Fields[Int32Value.ValueFieldNumber];

                dataContract = _innerContractResolver.GetDataContractForType(MessageDescriptorHelpers.ResolveFieldType(field));
                return true;
            }
            if (messageDescriptor.FullName == Timestamp.Descriptor.FullName ||
                messageDescriptor.FullName == Duration.Descriptor.FullName ||
                messageDescriptor.FullName == FieldMask.Descriptor.FullName)
            {
                dataContract = DataContract.ForPrimitive(messageDescriptor.ClrType, DataType.String, dataFormat: null);
                return true;
            }
            if (messageDescriptor.FullName == Struct.Descriptor.FullName)
            {
                dataContract = DataContract.ForObject(messageDescriptor.ClrType, Array.Empty<DataProperty>(), extensionDataType: typeof(Value));
                return true;
            }
            if (messageDescriptor.FullName == ListValue.Descriptor.FullName)
            {
                dataContract = DataContract.ForArray(messageDescriptor.ClrType, typeof(Value));
                return true;
            }
            if (messageDescriptor.FullName == Value.Descriptor.FullName)
            {
                dataContract = DataContract.ForPrimitive(messageDescriptor.ClrType, DataType.Unknown, dataFormat: null);
                return true;
            }
            if (messageDescriptor.FullName == Any.Descriptor.FullName)
            {
                var anyProperties = new List<DataProperty>
                {
                    new DataProperty("@type", typeof(string), isRequired: true)
                };
                dataContract = DataContract.ForObject(messageDescriptor.ClrType, anyProperties, extensionDataType: typeof(Value));
                return true;
            }
        }

        dataContract = null;
        return false;
    }

    private DataContract ConvertMessage(MessageDescriptor messageDescriptor)
    {
        if (TryCustomizeMessage(messageDescriptor, out var dataContract))
        {
            return dataContract;
        }

        var properties = new List<DataProperty>();

        foreach (var field in messageDescriptor.Fields.InFieldNumberOrder())
        {
            var fieldType = MessageDescriptorHelpers.ResolveFieldType(field);

            var propertyName = ServiceDescriptorHelpers.FormatUnderscoreName(field.Name, pascalCase: true, preservePeriod: false);
            var propertyInfo = messageDescriptor.ClrType.GetProperty(propertyName);

            properties.Add(new DataProperty(field.JsonName, fieldType, memberInfo: propertyInfo));
        }

        var schema = DataContract.ForObject(messageDescriptor.ClrType, properties: properties);

        return schema;
    }
}
