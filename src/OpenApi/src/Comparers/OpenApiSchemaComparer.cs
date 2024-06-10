// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiSchemaComparer : IEqualityComparer<OpenApiSchema>
{
    public static OpenApiSchemaComparer Instance { get; } = new OpenApiSchemaComparer();

    public bool Equals(OpenApiSchema? x, OpenApiSchema? y)
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

    public int GetHashCode(OpenApiSchema obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.AdditionalProperties, Instance);
        hashCode.Add(obj.AdditionalPropertiesAllowed);
        foreach (var item in obj.AllOf)
        {
            hashCode.Add(item, Instance);
        }
        foreach (var item in obj.AnyOf)
        {
            hashCode.Add(item, Instance);
        }
        hashCode.Add(obj.Deprecated);
        hashCode.Add(obj.Default, OpenApiAnyComparer.Instance);
        hashCode.Add(obj.Description);
        hashCode.Add(obj.Discriminator, OpenApiDiscriminatorComparer.Instance);
        hashCode.Add(obj.Example, OpenApiAnyComparer.Instance);
        hashCode.Add(obj.ExclusiveMaximum);
        hashCode.Add(obj.ExclusiveMinimum);
        foreach ((var key, var value) in obj.Extensions)
        {
            hashCode.Add(key);
            if (value is IOpenApiAny any)
            {
                hashCode.Add(any, OpenApiAnyComparer.Instance);
            }
            else
            {
                hashCode.Add(value);
            }
        }
        hashCode.Add(obj.ExternalDocs, OpenApiExternalDocsComparer.Instance);
        foreach (var item in obj.Enum)
        {
            hashCode.Add(item, OpenApiAnyComparer.Instance);
        }
        hashCode.Add(obj.Format);
        hashCode.Add(obj.Items, Instance);
        hashCode.Add(obj.Title);
        hashCode.Add(obj.Type);
        hashCode.Add(obj.Maximum);
        hashCode.Add(obj.MaxItems);
        hashCode.Add(obj.MaxLength);
        hashCode.Add(obj.MaxProperties);
        hashCode.Add(obj.Minimum);
        hashCode.Add(obj.MinItems);
        hashCode.Add(obj.MinLength);
        hashCode.Add(obj.MinProperties);
        hashCode.Add(obj.MultipleOf);
        foreach (var item in obj.OneOf)
        {
            hashCode.Add(item, Instance);
        }
        hashCode.Add(obj.Not, Instance);
        hashCode.Add(obj.Nullable);
        hashCode.Add(obj.Pattern);
        foreach ((var key, var value) in obj.Properties)
        {
            hashCode.Add(key);
            hashCode.Add(value, Instance);
        }
        hashCode.Add(obj.ReadOnly);
        foreach (var item in obj.Required.Order())
        {
            hashCode.Add(item);
        }
        hashCode.Add(obj.Reference, OpenApiReferenceComparer.Instance);
        hashCode.Add(obj.UniqueItems);
        hashCode.Add(obj.UnresolvedReference);
        hashCode.Add(obj.WriteOnly);
        hashCode.Add(obj.Xml, OpenApiXmlComparer.Instance);
        return hashCode.ToHashCode();
    }
}
