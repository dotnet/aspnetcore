// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi.Microbenchmarks;

public class OpenApiSchemaComparerBenchmark
{
    [Params(1, 10, 100)]
    public int ElementCount { get; set; }

    private OpenApiSchema _schema;

    [GlobalSetup(Target = nameof(OpenApiSchema_GetHashCode))]
    public void OpenApiSchema_Setup()
    {
        _schema = new OpenApiSchema
        {
            AdditionalProperties = GenerateInnerSchema(),
            AdditionalPropertiesAllowed = true,
            AllOf = Enumerable.Range(0, ElementCount).Select(_ => GenerateInnerSchema()).ToList(),
            AnyOf = Enumerable.Range(0, ElementCount).Select(_ => GenerateInnerSchema()).ToList(),
            Deprecated = true,
            Default = new OpenApiString("default"),
            Description = "description",
            Discriminator = new OpenApiDiscriminator(),
            Example = new OpenApiString("example"),
            ExclusiveMaximum = true,
            ExclusiveMinimum = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["key"] = new OpenApiString("value")
            },
            ExternalDocs = new OpenApiExternalDocs(),
            Enum = Enumerable.Range(0, ElementCount).Select(_ => (IOpenApiAny)new OpenApiString("enum")).ToList(),
            OneOf = Enumerable.Range(0, ElementCount).Select(_ => GenerateInnerSchema()).ToList(),
        };

        static OpenApiSchema GenerateInnerSchema() => new OpenApiSchema
        {
            Properties = Enumerable.Range(0, 10).ToDictionary(i => i.ToString(CultureInfo.InvariantCulture), _ => new OpenApiSchema()),
            Deprecated = true,
            Default = new OpenApiString("default"),
            Description = "description",
            Example = new OpenApiString("example"),
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["key"] = new OpenApiString("value")
            },
        };
    }

    [Benchmark]
    public void OpenApiSchema_GetHashCode()
    {
        OpenApiSchemaComparer.Instance.GetHashCode(_schema);
    }
}
