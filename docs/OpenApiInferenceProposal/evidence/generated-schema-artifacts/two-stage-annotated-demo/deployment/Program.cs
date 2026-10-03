// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Corvus.Text.Json.RuntimeEvaluator;
using GeneratedSchemaInspection;

var options = new JsonSchemaEvaluatorOptions
{
    DefaultDialect = JsonSchemaDialect.Draft202012,
    AssertFormat = false,
    AssertContent = true,
    CompileRegularExpressions = false,
    BaseUri = "urn:flagship:conditional",
};
using var evaluator = JsonSchemaEvaluator.FromProgramImage(FlagshipCorvusProgramImage.Bytes, options);
var valid = evaluator.Evaluate(
    """{"kind":"business","address":{"street":"1 High Street","city":"London"},"taxId":"GB123"}"""u8.ToArray());
var invalid = evaluator.Evaluate(
    """{"kind":"business","address":{"street":"1 High Street","city":"London"}}"""u8.ToArray());
if (!valid || invalid)
{
    throw new InvalidOperationException($"Unexpected image results: valid={valid}, invalid={invalid}.");
}

Console.WriteLine(
    $"{FlagshipCorvusProgramImage.SchemaGraphIdentity}:{FlagshipCorvusProgramImage.ImageIdentity}");
