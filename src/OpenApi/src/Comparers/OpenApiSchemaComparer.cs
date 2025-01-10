// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // If a local reference is present, we can't compare the schema directly
        // and should instead use the schema ID as a type-check to assert if the schemas are
        // equivalent.
        if ((x.Reference != null && y.Reference == null)
            || (x.Reference == null && y.Reference != null))
        {
            return SchemaIdEquals(x, y);
        }

        // Compare property equality in an order that should help us find inequality faster
        return
            x.Type == y.Type &&
            x.Format == y.Format &&
            SchemaIdEquals(x, y) &&
            ComparerHelpers.DictionaryEquals(x.Properties, y.Properties, Instance) &&
            OpenApiDiscriminatorComparer.Instance.Equals(x.Discriminator, y.Discriminator) &&
            Instance.Equals(x.AdditionalProperties, y.AdditionalProperties) &&
            x.AdditionalPropertiesAllowed == y.AdditionalPropertiesAllowed &&
            ComparerHelpers.ListEquals(x.AllOf, y.AllOf, Instance) &&
            ComparerHelpers.ListEquals(x.AnyOf, y.AnyOf, Instance) &&
            x.Deprecated == y.Deprecated &&
            OpenApiAnyComparer.Instance.Equals(x.Default, y.Default) &&
            x.Description == y.Description &&
            OpenApiAnyComparer.Instance.Equals(x.Example, y.Example) &&
            x.ExclusiveMaximum == y.ExclusiveMaximum &&
            x.ExclusiveMinimum == y.ExclusiveMinimum &&
            x.Extensions.Count == y.Extensions.Count &&
            ComparerHelpers.DictionaryEquals(x.Extensions, y.Extensions, OpenApiAnyComparer.Instance) &&
            OpenApiExternalDocsComparer.Instance.Equals(x.ExternalDocs, y.ExternalDocs) &&
            ComparerHelpers.ListEquals(x.Enum, y.Enum, OpenApiAnyComparer.Instance) &&
            Instance.Equals(x.Items, y.Items) &&
            x.Title == y.Title &&
            x.Maximum == y.Maximum &&
            x.MaxItems == y.MaxItems &&
            x.MaxLength == y.MaxLength &&
            x.MaxProperties == y.MaxProperties &&
            x.Minimum == y.Minimum &&
            x.MinItems == y.MinItems &&
            x.MinLength == y.MinLength &&
            x.MinProperties == y.MinProperties &&
            x.MultipleOf == y.MultipleOf &&
            ComparerHelpers.ListEquals(x.OneOf, y.OneOf, Instance) &&
            Instance.Equals(x.Not, y.Not) &&
            x.Nullable == y.Nullable &&
            x.Pattern == y.Pattern &&
            x.ReadOnly == y.ReadOnly &&
            x.Required.Count == y.Required.Count && x.Required.SetEquals(y.Required) &&
            OpenApiReferenceComparer.Instance.Equals(x.Reference, y.Reference) &&
            x.UniqueItems == y.UniqueItems &&
            x.UnresolvedReference == y.UnresolvedReference &&
            x.WriteOnly == y.WriteOnly &&
            OpenApiXmlComparer.Instance.Equals(x.Xml, y.Xml);
    }

    private static bool SchemaIdEquals(OpenApiSchema x, OpenApiSchema y)
    {
        if (x.Annotations == null && y.Annotations == null)
        {
            return true;
        }
        if (x.Annotations == null || y.Annotations == null)
        {
            return false;
        }
        if (x.Annotations.TryGetValue(OpenApiConstants.SchemaId, out var xSchemaId)
            && y.Annotations.TryGetValue(OpenApiConstants.SchemaId, out var ySchemaId))
        {
            if (xSchemaId == null && ySchemaId == null)
            {
                return true;
            }
            if (xSchemaId == null || ySchemaId == null)
            {
                return false;
            }
            return xSchemaId.Equals(ySchemaId);
        }
        return true;
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
