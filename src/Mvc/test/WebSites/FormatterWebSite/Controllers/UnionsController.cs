// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Produces("application/json")]
public class UnionsController : ControllerBase
{
    // ------------------------------------------------------------
    // Output: primitive-paired unions, one endpoint per primitive type. The route
    // parameter selects which case to construct so a single Theory can exercise both
    // cases for every type.
    // ------------------------------------------------------------

    [HttpGet("{kind}")]
    public UnionByteString PrimitiveByteString(string kind) => kind switch
    {
        "value" => new UnionByteString((byte)42),
        "string" => new UnionByteString("hi"),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionShortString PrimitiveShortString(string kind) => kind switch
    {
        "value" => new UnionShortString((short)1234),
        "string" => new UnionShortString("hi"),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionIntString PrimitiveIntString(string kind) => kind switch
    {
        "value" => new UnionIntString(42),
        "string" => new UnionIntString("hi"),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionLongString PrimitiveLongString(string kind) => kind switch
    {
        "value" => new UnionLongString(9999999999L),
        "string" => new UnionLongString("hi"),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionDecimalString PrimitiveDecimalString(string kind) => kind switch
    {
        "value" => new UnionDecimalString(3.14m),
        "string" => new UnionDecimalString("hi"),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionDoubleString PrimitiveDoubleString(string kind) => kind switch
    {
        "value" => new UnionDoubleString(2.5),
        "string" => new UnionDoubleString("hi"),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionBoolString PrimitiveBoolString(string kind) => kind switch
    {
        "value" => new UnionBoolString(true),
        "string" => new UnionBoolString("hi"),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionGuidInt PrimitiveGuidInt(string kind) => kind switch
    {
        "value" => new UnionGuidInt(new Guid("00000000-0000-0000-0000-000000000001")),
        "int" => new UnionGuidInt(42),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionDateTimeInt PrimitiveDateTimeInt(string kind) => kind switch
    {
        "value" => new UnionDateTimeInt(new DateTime(2024, 5, 28, 10, 0, 0, DateTimeKind.Unspecified)),
        "int" => new UnionDateTimeInt(42),
        _ => default,
    };

    [HttpGet("{kind}")]
    public UnionCharInt PrimitiveCharInt(string kind) => kind switch
    {
        "value" => new UnionCharInt('x'),
        "int" => new UnionCharInt(42),
        _ => default,
    };

    // ------------------------------------------------------------
    // Output: Task<Union> and ValueTask<Union> async return types.
    // ------------------------------------------------------------

    [HttpGet]
    public Task<UnionIntString> AsyncTask() => Task.FromResult(new UnionIntString(42));

    [HttpGet]
    public ValueTask<UnionIntString> AsyncValueTask() => ValueTask.FromResult(new UnionIntString(42));

    // ------------------------------------------------------------
    // Output: nullable union wrapper (Union?) — value and null cases.
    // ------------------------------------------------------------

    [HttpGet("{kind}")]
    public UnionIntString? NullableWrapper(string kind) => kind switch
    {
        "value" => (UnionIntString?)new UnionIntString(42),
        "null" => (UnionIntString?)null,
        _ => (UnionIntString?)null,
    };

    // ------------------------------------------------------------
    // Output: union with a nullable case.
    // ------------------------------------------------------------

    [HttpGet("{kind}")]
    public UnionNullableIntString UnionWithNullableCase(string kind) => kind switch
    {
        "int" => new UnionNullableIntString(5),
        "string" => new UnionNullableIntString("hi"),
        _ => default,
    };

    // ------------------------------------------------------------
    // Output: object-case union — dispatch by runtime .NET type.
    // ------------------------------------------------------------

    [HttpGet("{kind}")]
    public UnionPet ObjectCase(string kind) => kind switch
    {
        "cat" => new UnionPet(new Cat("Whiskers")),
        "dog" => new UnionPet(new Dog("Labrador")),
        _ => default,
    };

    // ------------------------------------------------------------
    // Output: nested union — UnionOuter wraps UnionInner.
    // ------------------------------------------------------------

    [HttpGet("{kind}")]
    public UnionOuter Nested(string kind) => kind switch
    {
        "int" => new UnionOuter(new UnionInner(42)),
        "string" => new UnionOuter(new UnionInner("nested")),
        "bool" => new UnionOuter(true),
        _ => default,
    };

    // ------------------------------------------------------------
    // Output: union as a property of a wrapping record (envelope shape).
    // ------------------------------------------------------------

    [HttpGet("{kind}")]
    public UnionEnvelope Envelope(string kind) => kind switch
    {
        "int" => new UnionEnvelope("abc", new UnionIntString(42)),
        "string" => new UnionEnvelope("abc", new UnionIntString("hi")),
        _ => default,
    };

    // ------------------------------------------------------------
    // Input + output: echo endpoints. Each takes a union body parameter and
    // returns it as the response so one test exercises both the input formatter
    // (deserialize) and the output formatter (serialize) for the same scenario.
    // ------------------------------------------------------------

    [HttpPost]
    public UnionBoolString EchoBoolString([FromBody] UnionBoolString value) => value;

    [HttpPost]
    public UnionIntString EchoIntString([FromBody] UnionIntString value) => value;

    [HttpPost]
    public UnionIntStringWithClassifier EchoIntStringWithClassifier([FromBody] UnionIntStringWithClassifier value) => value;

    [HttpPost]
    public UnionIntString EchoIntStringImplicit(UnionIntString value) => value;

    [HttpPost]
    public UnionIntShort EchoIntShort([FromBody] UnionIntShort value) => value;

    [HttpPost]
    public UnionIntShortWithClassifier EchoIntShortWithClassifier([FromBody] UnionIntShortWithClassifier value) => value;

    [HttpPost]
    public UnionNullableIntString EchoNullableIntString([FromBody] UnionNullableIntString value) => value;

    [HttpPost]
    public UnionNullableIntStringWithClassifier EchoNullableIntStringWithClassifier([FromBody] UnionNullableIntStringWithClassifier value) => value;

    [HttpPost]
    public UnionPet EchoPet([FromBody] UnionPet value) => value;

    [HttpPost]
    public UnionPetWithClassifier EchoPetWithClassifier([FromBody] UnionPetWithClassifier value) => value;

    [HttpPost]
    public UnionEnvelope EchoEnvelope([FromBody] UnionEnvelope value) => value;
}
