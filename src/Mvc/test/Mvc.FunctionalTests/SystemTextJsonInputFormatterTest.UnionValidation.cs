// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public partial class SystemTextJsonInputFormatterTest
{
    [Theory]
    [InlineData("ValidateAdmin", "{\"adminId\":\"A1\",\"role\":\"SuperAdminRoleExceeding10\"}", "Role", "The field Role must be a string or array type with a maximum length of '10'.")]
    [InlineData("ValidateUser", "{\"adminId\":\"A1\",\"role\":\"SuperAdminRoleExceeding10\"}", "Value.Role", "The field Role must be a string or array type with a maximum length of '10'.")]
    [InlineData("ValidateUser", "{\"adminId\":null,\"role\":\"Admin\"}", "Value.AdminId", "The AdminId field is required.")]
    [InlineData("ValidateUser", "{\"customerId\":\"C1\",\"age\":17}", "Value.Age", "The field Age must be between 18 and 120.")]
    [InlineData("ValidateUser", "{\"customerId\":null,\"age\":18}", "Value.CustomerId", "The CustomerId field is required.")]
    [InlineData("ValidateUserEnvelope", "{\"user\":{\"adminId\":\"A1\",\"role\":\"SuperAdminRoleExceeding10\"}}", "User.Value.Role", "The field Role must be a string or array type with a maximum length of '10'.")]
    [InlineData("ValidateOptionalUserEnvelope", "{\"user\":{\"customerId\":\"C1\",\"age\":121}}", "User.Value.Age", "The field Age must be between 18 and 120.")]
    [InlineData("ValidateRequiredNullableUserEnvelope", "{}", "User", "The User field is required.")]
    [InlineData("ValidateRequiredNullableUserEnvelope", "{\"user\":null}", "User", "The User field is required.")]
    [InlineData("ValidateUsers", "[{\"adminId\":\"A1\",\"role\":\"Admin\"},{\"customerId\":\"C1\",\"age\":17}]", "[1].Value.Age", "The field Age must be between 18 and 120.")]
    [InlineData("ValidateNestedUser", "{\"customerId\":\"C1\",\"age\":17}", "Value.Value.Age", "The field Age must be between 18 and 120.")]
    public async Task UnionValidation_InvalidCase_ReturnsAutomaticBadRequest(
        string action, string payload, string errorKey, string errorMessage)
    {
        var response = await Client.PostAsync($"/Unions/{action}", JsonBody(payload));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        var error = Assert.Single(problem.Errors);
        Assert.Equal(errorKey, error.Key);
        Assert.Equal(errorMessage, Assert.Single(error.Value));
    }

    [Theory]
    [InlineData("ValidateUser", "{\"adminId\":\"A1\",\"role\":\"Admin\"}")]
    [InlineData("ValidateUser", "{\"customerId\":\"C1\",\"age\":18}")]
    [InlineData("ValidateUserEnvelope", "{\"user\":{\"customerId\":\"C1\",\"age\":120}}")]
    [InlineData("ValidateOptionalUserEnvelope", "{\"user\":{\"adminId\":\"A1\",\"role\":\"Admin\"}}")]
    [InlineData("ValidateRequiredNullableUserEnvelope", "{\"user\":{\"adminId\":\"A1\",\"role\":\"Admin\"}}")]
    [InlineData("ValidateUsers", "[{\"adminId\":\"A1\",\"role\":\"Admin\"},{\"customerId\":\"C1\",\"age\":18}]")]
    [InlineData("ValidateNestedUser", "{\"adminId\":\"A1\",\"role\":\"Admin\"}")]
    [InlineData("ValidateNestedUser", "true")]
    [InlineData("ValidateUserEnvelope", "{}")]
    [InlineData("ValidateUserEnvelope", "{\"user\":null}")]
    [InlineData("ValidateOptionalUserEnvelope", "{}")]
    [InlineData("ValidateOptionalUserEnvelope", "{\"user\":null}")]
    [InlineData("ValidateSuppressedUserEnvelope", "{\"user\":{\"customerId\":\"C1\",\"age\":17}}")]
    public async Task UnionValidation_ValidDefaultOrSuppressedCase_IsAccepted(string action, string payload)
    {
        var response = await Client.PostAsync($"/Unions/{action}", JsonBody(payload));

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadFromJsonAsync<bool>());
    }

    [Theory]
    [InlineData(typeof(ValidatedUser))]
    [InlineData(typeof(UnionCustomer))]
    public async Task UnionValidation_SuppressedChildValidation_IsAccepted(Type suppressedType)
    {
        using var factory = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Configure<MvcOptions>(options =>
                options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(suppressedType)))));
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/Unions/ValidateUser", JsonBody("{\"customerId\":\"C1\",\"age\":17}"));

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadFromJsonAsync<bool>());
    }
}
