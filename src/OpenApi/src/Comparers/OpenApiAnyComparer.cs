// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Any;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiAnyComparer : IEqualityComparer<IOpenApiAny>
{
    public static OpenApiAnyComparer Instance { get; } = new OpenApiAnyComparer();

    public bool Equals(IOpenApiAny? x, IOpenApiAny? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }

        return GetHashCode(x) == GetHashCode(y);
    }

    public int GetHashCode(IOpenApiAny obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.AnyType);
        if (obj is IOpenApiPrimitive primitive)
        {
            hashCode.Add(primitive.PrimitiveType);
        }
        if (obj is OpenApiArray array)
        {
            foreach (var item in array)
            {
                hashCode.Add(item, Instance);
            }
        }
        if (obj is OpenApiObject openApiObject)
        {
            foreach (var item in openApiObject)
            {
                hashCode.Add(item.Key);
                hashCode.Add(item.Value, Instance);
            }
        }
        hashCode.Add<object?>(obj switch {
            OpenApiBinary binary => binary.Value,
            OpenApiInteger integer => integer.Value,
            OpenApiLong @long => @long.Value,
            OpenApiDouble @double => @double.Value,
            OpenApiFloat @float => @float.Value,
            OpenApiBoolean boolean => boolean.Value,
            OpenApiString @string => @string.Value,
            OpenApiPassword password => password.Value,
            OpenApiByte @byte => @byte.Value,
            OpenApiDate date => date.Value,
            OpenApiDateTime dateTime => dateTime.Value,
            _ => null
        });

        return hashCode.ToHashCode();
    }
}
