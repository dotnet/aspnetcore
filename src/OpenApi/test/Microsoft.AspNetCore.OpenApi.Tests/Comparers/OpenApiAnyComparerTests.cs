// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;

public class OpenApiAnyComparerTests
{
    public static object[][] Data => [
        [new OpenApiNull(), new OpenApiNull(), true],
        [new OpenApiNull(), new OpenApiBoolean(true), false],
        [new OpenApiByte(1), new OpenApiByte(1), true],
        [new OpenApiByte(1), new OpenApiByte(2), false],
        [new OpenApiBinary(Encoding.UTF8.GetBytes("test")), new OpenApiBinary(Encoding.UTF8.GetBytes("test")), true],
        [new OpenApiBinary(Encoding.UTF8.GetBytes("test2")), new OpenApiBinary(Encoding.UTF8.GetBytes("test")), false],
        [new OpenApiBoolean(true), new OpenApiBoolean(true), true],
        [new OpenApiBoolean(true), new OpenApiBoolean(false), false],
        [new OpenApiInteger(1), new OpenApiInteger(1), true],
        [new OpenApiInteger(1), new OpenApiInteger(2), false],
        [new OpenApiInteger(1), new OpenApiLong(1), false],
        [new OpenApiLong(1), new OpenApiLong(1), true],
        [new OpenApiLong(1), new OpenApiLong(2), false],
        [new OpenApiFloat(1.1f), new OpenApiFloat(1.1f), true],
        [new OpenApiFloat(1.1f), new OpenApiFloat(1.2f), false],
        [new OpenApiDouble(1.1), new OpenApiDouble(1.1), true],
        [new OpenApiDouble(1.1), new OpenApiDouble(1.2), false],
        [new OpenApiString("value"), new OpenApiString("value"), true],
        [new OpenApiString("value"), new OpenApiString("value2"), false],
        [new OpenApiObject(), new OpenApiObject(), true],
        [new OpenApiObject(), new OpenApiObject { ["key"] = new OpenApiString("value") }, false],
        [new OpenApiObject { ["key"] = new OpenApiString("value") }, new OpenApiObject { ["key"] = new OpenApiString("value") }, true],
        [new OpenApiObject { ["key"] = new OpenApiString("value") }, new OpenApiObject { ["key"] = new OpenApiString("value2") }, false],
        [new OpenApiObject { ["key2"] = new OpenApiString("value") }, new OpenApiObject { ["key"] = new OpenApiString("value") }, false],
        [new OpenApiDate(DateTime.Today), new OpenApiDate(DateTime.Today), true],
        [new OpenApiDate(DateTime.Today), new OpenApiDate(DateTime.Today.AddDays(1)), false],
        [new OpenApiPassword("password"), new OpenApiPassword("password"), true],
        [new OpenApiPassword("password"), new OpenApiPassword("password2"), false],
        [new OpenApiArray { new OpenApiString("value") }, new OpenApiArray { new OpenApiString("value") }, true],
        [new OpenApiArray { new OpenApiString("value") }, new OpenApiArray { new OpenApiString("value2") }, false],
        [new OpenApiArray { new OpenApiString("value2"), new OpenApiString("value") }, new OpenApiArray { new OpenApiString("value"), new OpenApiString("value2") }, false],
        [new OpenApiArray { new OpenApiString("value"), new OpenApiString("value") }, new OpenApiArray { new OpenApiString("value"), new OpenApiString("value") }, true],
        [new ScrubbedOpenApiAny("value"), new ScrubbedOpenApiAny("value"), true],
        [new ScrubbedOpenApiAny("value"), new ScrubbedOpenApiAny("value2"), false],
        [new ScrubbedOpenApiAny(null), new ScrubbedOpenApiAny(null), true]
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public void ProducesCorrectEqualityForOpenApiAny(IOpenApiAny any, IOpenApiAny anotherAny, bool isEqual)
        => Assert.Equal(isEqual, OpenApiAnyComparer.Instance.Equals(any, anotherAny));
}
