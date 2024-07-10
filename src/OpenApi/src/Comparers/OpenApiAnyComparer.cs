// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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
        if (object.ReferenceEquals(x, y))
        {
            return true;
        }

        return x.AnyType == y.AnyType &&
            (x switch
            {
                OpenApiNull _ => y is OpenApiNull,
                OpenApiArray arrayX => y is OpenApiArray arrayY && arrayX.SequenceEqual(arrayY, Instance),
                OpenApiObject objectX => y is OpenApiObject objectY && objectX.Keys.Count == objectY.Keys.Count && objectX.Keys.All(key => objectY.ContainsKey(key) && Equals(objectX[key], objectY[key])),
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
                ScrubbedOpenApiAny scrubbedX => y is ScrubbedOpenApiAny scrubbedY && scrubbedX.Value == scrubbedY.Value,
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
            ScrubbedOpenApiAny scrubbed => scrubbed.Value,
            _ => null
        });

        return hashCode.ToHashCode();
    }
}
