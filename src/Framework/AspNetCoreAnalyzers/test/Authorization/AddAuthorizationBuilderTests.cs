// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.Authorization.AddAuthorizationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.Authorization.Fixers.AddAuthorizationBuilderFixer>;

namespace Microsoft.AspNetCore.Analyzers.Authorization;

public sealed class AddAuthorizationBuilderTests
{
    // TODO: Additional Test Cases
    // - Refactoring to call InvokeHandlersAfterFailure instead of using setter
    // - Other IServiceCollection extension is changed to AddAuthorization call.
    //   to keep things simple, just check if the parent of the InvocationExpression is a GlobalStatementExpression

    [Fact]
    public async Task AddAuthorization_WithSingleAddPolicyCall_UsingExpressionBody_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
})|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_WithMultipleAddPolicyCalls_UsingExpressionBody_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(""AtLeast18"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));

    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
})|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(""AtLeast18"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)))
    .AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_WithSingleAddPolicyCall_UsingBlockBody_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
})|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_WithMultipleAddPolicyCalls_UsingBlockBody_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(""AtLeast18"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(18));
    });

    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
})|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(""AtLeast18"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(18));
    })
    .AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_WithAuthorizationOptionsDefaultPolicyAssignment_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireClaim(""Claim"")
        .Build();

    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
})|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireClaim(""Claim"")
        .Build())
    .AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_WithAuthorizationOptionsFallbackPolicyAssignment_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireClaim(""Claim"")
        .Build();

    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
})|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireClaim(""Claim"")
        .Build())
    .AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_WithAuthorizationOptionsInvokeHandlersAfterFailureAssignment_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(options =>
{
    options.InvokeHandlersAfterFailure = false;

    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
})|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .SetInvokeHandlersAfterFailure(false)
    .AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_AccessesAuthorizationOptionsDefaultPolicy_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    _ = options.DefaultPolicy;
    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AddAuthorization_AccessesAuthorizationOptionsFallbackPolicy_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    _ = options.FallbackPolicy;
    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AddAuthorization_AccessesAuthorizationOptionsInvokeHandlesAfterFailure_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    _ = options.InvokeHandlersAfterFailure;
    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AddAuthorization_ReferencesAuthorizationOptionsGetPolicy_NoDiagnostic()
    {
        var source = @"
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    Func<string, AuthorizationPolicy?> _ = options.GetPolicy;
    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AddAuthorization_InvokesAuthorizationOptionsGetPolicy_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    _ = options.GetPolicy("""");
    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
});
";

        await VerifyNoCodeFix(source);
    }

    private static async Task VerifyCodeFix(string source, DiagnosticResult[] diagnostics, string fixedSource)
    {
        var fullSource = string.Join(Environment.NewLine, source, _testAuthorizationPolicyClassDeclaration);
        var fullFixedSource = string.Join(Environment.NewLine, fixedSource, _testAuthorizationPolicyClassDeclaration);

        await VerifyCS.VerifyCodeFixAsync(fullSource, diagnostics, fullFixedSource);
    }

    private static async Task VerifyNoCodeFix(string source)
    {
        var fullSource = string.Join(Environment.NewLine, source, _testAuthorizationPolicyClassDeclaration);

        await VerifyCS.VerifyCodeFixAsync(fullSource, Array.Empty<DiagnosticResult>(), fullSource);
    }

    private const string _testAuthorizationPolicyClassDeclaration = @"
public class MinimumAgeRequirement: IAuthorizationRequirement
{
    public int Age { get; }
    public MinimumAgeRequirement(int age) => Age = age;
}
";
}
