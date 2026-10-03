// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;

public class OpenApiSchemaExtensionsTests
{
    [Fact]
    public void MakeArrayItemsNullable_DoesNotWrapAlreadyNullableOneOfItemSchema()
    {
        var existingNullableItemsSchema = new OpenApiSchema
        {
            OneOf =
            [
                new OpenApiSchema { Type = JsonSchemaType.Null },
                new OpenApiSchema { Type = JsonSchemaType.String }
            ]
        };

        var arraySchema = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = existingNullableItemsSchema,
        };

        arraySchema.MakeArrayItemsNullable();

        Assert.Same(existingNullableItemsSchema, arraySchema.Items);
        var itemSchema = Assert.IsType<OpenApiSchema>(arraySchema.Items);
        Assert.Collection(itemSchema.OneOf,
            item => Assert.Equal(JsonSchemaType.Null, item.Type),
            item => Assert.Equal(JsonSchemaType.String, item.Type));
    }
}
