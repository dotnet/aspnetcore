// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiAnyComparer : IEqualityComparer<IOpenApiAny>, IEqualityComparer<IOpenApiExtension>
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
        if (object.ReferenceEquals(x, y))
        {
            return true;
        }

        return x.AnyType == y.AnyType &&
            (x switch
            {
                OpenApiNull _ => y is OpenApiNull,
                OpenApiArray arrayX => y is OpenApiArray arrayY && ComparerHelpers.ListEquals(arrayX, arrayY, Instance),
                OpenApiObject objectX => y is OpenApiObject objectY && ComparerHelpers.DictionaryEquals(objectX, objectY, Instance),
                OpenApiBinary binaryX => y is OpenApiBinary binaryY && binaryX.Value.SequenceEqual(binaryY.Value),
                OpenApiInteger integerX => y is OpenApiInteger integerY && integerX.Value == integerY.Value,
                OpenApiLong longX => y is OpenApiLong longY && longX.Value == longY.Value,
                OpenApiDouble doubleX => y is OpenApiDouble doubleY && doubleX.Value == doubleY.Value,
                OpenApiFloat floatX => y is OpenApiFloat floatY && floatX.Value == floatY.Value,
                OpenApiBoolean booleanX => y is OpenApiBoolean booleanY && booleanX.Value == booleanY.Value,
                OpenApiString stringX => y is OpenApiString stringY && stringX.Value == stringY.Value,
                OpenApiPassword passwordX => y is OpenApiPassword passwordY && passwordX.Value == passwordY.Value,
                OpenApiByte byteX => y is OpenApiByte byteY && byteX.Value.SequenceEqual(byteY.Value),
                OpenApiDate dateX => y is OpenApiDate dateY && dateX.Value == dateY.Value,
                OpenApiDateTime dateTimeX => y is OpenApiDateTime dateTimeY && dateTimeX.Value == dateTimeY.Value,
                _ => x.Equals(y)
            });
    }

    public int GetHashCode(IOpenApiAny obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.AnyType);
        if (obj is IOpenApiPrimitive primitive)
        {
            hashCode.Add(primitive.PrimitiveType);
        }
        if (obj is OpenApiBinary binary)
        {
            hashCode.AddBytes(binary.Value);
        }
        if (obj is OpenApiByte bytes)
        {
            hashCode.AddBytes(bytes.Value);
        }
        hashCode.Add<object?>(obj switch
        {
            OpenApiInteger integer => integer.Value,
            OpenApiLong @long => @long.Value,
            OpenApiDouble @double => @double.Value,
            OpenApiFloat @float => @float.Value,
            OpenApiBoolean boolean => boolean.Value,
            OpenApiString @string => @string.Value,
            OpenApiPassword password => password.Value,
            OpenApiDate date => date.Value,
            OpenApiDateTime dateTime => dateTime.Value,
            _ => null
        });

        return hashCode.ToHashCode();
    }

    public bool Equals(IOpenApiExtension? x, IOpenApiExtension? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        if (object.ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is IOpenApiAny openApiAnyX && y is IOpenApiAny openApiAnyY)
        {
            return Equals(openApiAnyX, openApiAnyY);
        }

        return x.Equals(y);
    }

    public int GetHashCode(IOpenApiExtension obj)
    {
        if (obj is IOpenApiAny any)
        {
            return GetHashCode(any);
        }

        return obj.GetHashCode();
    }
}
