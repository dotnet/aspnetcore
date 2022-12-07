// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Policy;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer>;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class DisallowNonParsableComplexTypesOnParametersTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RouteHandlerAnalyzer());

    [Fact]
    public async Task Route_Parameter_withoutComplexTypes_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/{name}"", (string name) => {});
";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task QueryString_Parameter_withString_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (string name) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withNullableString_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (string? name) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withStringArray_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (string[] names) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withUri_Works()
    {
        // Arrange
        var source = $$"""
using System;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (Uri url) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withNullableUri_Works()
    {
        // Arrange
        var source = $$"""
using System;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (Uri? url) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withUriArray_Works()
    {
        // Arrange
        var source = $$"""
using System;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (Uri[] url) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withInt_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (int pageIndex) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withNullableInt_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (int? pageIndex) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task QueryString_Parameter_withIntArray_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/", (int[] id) => {});
""";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Route_Parameter_withNonParsableComplexType_Fails()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/customers/{customer}", ({|#0:Customer customer|}) => {});

public class Customer
{
}
""";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsableOrBindable)
            .WithArguments("customer", "Customer")
            .WithLocation(0);

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostic);
    }

    [Fact]
    public async Task Route_Parameter_withBindAsyncMethodThatReturnsTask_of_T_Fails()
    {
        // Arrange
        var source = $$"""
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet("/customers/{customer}", ({|#0:Customer customer|}) => {});

public class Customer
{
    public async static Task<Customer> BindAsync(HttpContext context)
    {
        return new Customer();
    }
}
""";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.BindAsyncSignatureMustReturnValueTaskOfT)
            .WithArguments("customer", "Customer")
            .WithLocation(0);

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostic);
    }

    [Fact]
    public async Task Route_Parameter_withParsableComplexType_viaImplicitIParsable_Works()
    {
        // Arrange
        var source = $$"""
using System;
using Microsoft.AspNetCore.Builder;

var webApp = WebApplication.Create();
webApp.MapGet("/customers/{customer}", (Customer customer) => {});

public class Customer : IParsable<Customer>
{
    public static Customer Parse(string s, IFormatProvider provider)
    {
        if (TryParse(s, provider, out Customer customer))
        {
            return customer;
        }
        else
        {
            throw new ArgumentException(s);
        }
    }

    public static bool TryParse(string s, IFormatProvider provider, out Customer result)
    {
        result = new Customer();
        return true;
    }
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Route_Parameter_withParsableComplexType_viaExplicitIParsable_Works()
    {
        // Arrange
        var source = $$"""
using System;
using Microsoft.AspNetCore.Builder;

var webApp = WebApplication.Create();
webApp.MapGet("/customers/{customer}", (Customer customer) => {});

public class Customer : IParsable<Customer>
{
    static Customer IParsable<Customer>.Parse(string s, IFormatProvider provider)
    {
        if (TryParse(s, provider, out Customer customer))
        {
            return customer;
        }
        else
        {
            throw new ArgumentException(s);
        }
    }

    static bool IParsable<Customer>.TryParse(string s, IFormatProvider? provider, out Customer result)
    {
        return TryParse(s, provider, out result);
    }

    // HACK: Can't call IParsable<Customer>.TryParse(...) from IParsable<Customer>.Parse(...)
    private static bool TryParse(string s, IFormatProvider? provider, out Customer result)
    {
        result = new Customer();
        return true;
    }
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Route_Parameter_withParsableComplexType_viaMethodConvention_Works()
    {
        // Arrange
        var source = $$"""
using System;
using Microsoft.AspNetCore.Builder;

var webApp = WebApplication.Create();
webApp.MapPost("/customers/{customer}/contacts", (Customer customer) => {});

public class Customer
{
    public static bool TryParse(string s, IFormatProvider provider, out Customer result)
    {
        result = new Customer();
        return true;
    }
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Route_Parameter_withHttpContextBindableComplexType_viaImplicitIBindableFromHttp_Works()
    {
        // Arrange
        var source = $$"""
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var webApp = WebApplication.Create();
webApp.MapGet("/customers/{customer}", (Customer customer) => {});

public class Customer : IBindableFromHttpContext<Customer>
{
    public static async ValueTask<Customer?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        return new Customer();
    }
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Route_Parameter_withHttpContextBindableComplexType_viaExplicitIBindableFromHttp_Works()
    {
        // Arrange
        var source = $$"""
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var webApp = WebApplication.Create();
webApp.MapGet("/customers/{customer}", (Customer customer) => {});

public class Customer : IBindableFromHttpContext<Customer>
{
    static async ValueTask<Customer?> IBindableFromHttpContext<Customer>.BindAsync(HttpContext context, ParameterInfo parameter)
    {
        return new Customer();
    }
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Route_Parameter_withNullableType_Works()
    {
        // Arrange
        var source = $$"""
using System;
using Microsoft.AspNetCore.Builder;

var webApp = WebApplication.Create();
webApp.MapGet("/customers/{customer}/contacts", (int? customer) => {});
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Handler_Parameter_withFromBodyAttribute_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;

var webApp = WebApplication.Create();
webApp.MapPost(
    "/customers",
    ([FromBody]Customer customer) => {
    });

public class Customer
{
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Handler_Parameter_withBindableComplexType_viaMethodConvention_Works()
    {
        // Arrange
        var source = $$"""
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;

var webApp = WebApplication.Create();
webApp.MapPost(
    "/customers",
    ([FromBody]Customer customer) => {
    });

public class Customer
{
    public static async ValueTask<Customer?> BindAsync(HttpContext context)
    {
        return new Customer();
    }
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Handler_Parameter_withBindableComplexType_viaMethodConventionWithParameterInfo_Works()
    {
        // Arrange
        var source = $$"""
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;

var webApp = WebApplication.Create();
webApp.MapPost(
    "/customers",
    ([FromBody]Customer customer) => {
    });

public class Customer
{
    public static async ValueTask<Customer?> BindAsync(HttpContext context, ParameterInfo parameterInfo)
    {
        return new Customer();
    }
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Handler_Parameter_withWellknownTypes_Works()
    {
        // Arrange
        var source = $$"""
using System;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var webApp = WebApplication.Create();
webApp.MapPost(
    "/customers",
    (CancellationToken cancellationToken,
     HttpContext context,
     HttpRequest request,
     HttpResponse response,
     ClaimsPrincipal claimsPrincipal,
     IFormFileCollection formFileCollection,
     IFormFile formFile,
     Stream stream,
     PipeReader pipeReader) => {
     });
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Handler_Parameter_with_FromService_Attribute_Works()
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();

webApp.MapPost(
    "/customers",
    ([FromServices]MyService myService) => {

    });

public class MyService
{
}
""";

        // Act
        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}

