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
        if (object.ReferenceEquals(x, y))
        {
            return true;
        }

        return Instance.Equals(x.AdditionalProperties, y.AdditionalProperties) &&
            x.AdditionalPropertiesAllowed == y.AdditionalPropertiesAllowed &&
            x.AllOf.SequenceEqual(y.AllOf, Instance) &&
            x.AnyOf.SequenceEqual(y.AnyOf, Instance) &&
            x.Deprecated == y.Deprecated &&
            OpenApiAnyComparer.Instance.Equals(x.Default, y.Default) &&
            x.Description == y.Description &&
            OpenApiDiscriminatorComparer.Instance.Equals(x.Discriminator, y.Discriminator) &&
            OpenApiAnyComparer.Instance.Equals(x.Example, y.Example) &&
            x.ExclusiveMaximum == y.ExclusiveMaximum &&
            x.ExclusiveMinimum == y.ExclusiveMinimum &&
            x.Extensions.Count == y.Extensions.Count
            && x.Extensions.Keys.All(k => y.Extensions.ContainsKey(k) && x.Extensions[k] is IOpenApiAny anyX && y.Extensions[k] is IOpenApiAny anyY && OpenApiAnyComparer.Instance.Equals(anyX, anyY)) &&
            OpenApiExternalDocsComparer.Instance.Equals(x.ExternalDocs, y.ExternalDocs) &&
            x.Enum.SequenceEqual(y.Enum, OpenApiAnyComparer.Instance) &&
            x.Format == y.Format &&
            Instance.Equals(x.Items, y.Items) &&
            x.Title == y.Title &&
            x.Type == y.Type &&
            x.Maximum == y.Maximum &&
            x.MaxItems == y.MaxItems &&
            x.MaxLength == y.MaxLength &&
            x.MaxProperties == y.MaxProperties &&
            x.Minimum == y.Minimum &&
            x.MinItems == y.MinItems &&
            x.MinLength == y.MinLength &&
            x.MinProperties == y.MinProperties &&
            x.MultipleOf == y.MultipleOf &&
            x.OneOf.SequenceEqual(y.OneOf, Instance) &&
            Instance.Equals(x.Not, y.Not) &&
            x.Nullable == y.Nullable &&
            x.Pattern == y.Pattern &&
            x.Properties.Keys.All(k => y.Properties.ContainsKey(k) && Instance.Equals(x.Properties[k], y.Properties[k])) &&
            x.ReadOnly == y.ReadOnly &&
            x.Required.Order().SequenceEqual(y.Required.Order()) &&
            OpenApiReferenceComparer.Instance.Equals(x.Reference, y.Reference) &&
            x.UniqueItems == y.UniqueItems &&
            x.UnresolvedReference == y.UnresolvedReference &&
            x.WriteOnly == y.WriteOnly &&
            OpenApiXmlComparer.Instance.Equals(x.Xml, y.Xml);
    }

    public int GetHashCode(OpenApiSchema obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.AdditionalProperties, Instance);
        hashCode.Add(obj.AdditionalPropertiesAllowed);
        hashCode.Add(obj.AllOf.Count);
        hashCode.Add(obj.AnyOf.Count);
        hashCode.Add(obj.Deprecated);
        hashCode.Add(obj.Default, OpenApiAnyComparer.Instance);
        hashCode.Add(obj.Description);
        hashCode.Add(obj.Discriminator, OpenApiDiscriminatorComparer.Instance);
        hashCode.Add(obj.Example, OpenApiAnyComparer.Instance);
        hashCode.Add(obj.ExclusiveMaximum);
        hashCode.Add(obj.ExclusiveMinimum);
        hashCode.Add(obj.Extensions.Count);
        hashCode.Add(obj.ExternalDocs, OpenApiExternalDocsComparer.Instance);
        hashCode.Add(obj.Enum.Count);
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
        hashCode.Add(obj.OneOf.Count);
        hashCode.Add(obj.Not, Instance);
        hashCode.Add(obj.Nullable);
        hashCode.Add(obj.Pattern);
        hashCode.Add(obj.Properties.Count);
        hashCode.Add(obj.ReadOnly);
        hashCode.Add(obj.Required.Count);
        hashCode.Add(obj.Reference, OpenApiReferenceComparer.Instance);
        hashCode.Add(obj.UniqueItems);
        hashCode.Add(obj.UnresolvedReference);
        hashCode.Add(obj.WriteOnly);
        hashCode.Add(obj.Xml, OpenApiXmlComparer.Instance);
        return hashCode.ToHashCode();
    }
}
