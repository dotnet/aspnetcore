// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.Authorization.AddAuthorizationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.Authorization.Fixers.AddAuthorizationBuilderFixer>;

namespace Microsoft.AspNetCore.Analyzers.Authorization;

public sealed class AddAuthorizationBuilderTests
{
    [Fact]
    public async Task ConfigureAction_UsingExpressionBody_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddAuthorization(configure: options =>
    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21))))|};
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
    public async Task SingleAddPolicyCall_UsingExpressionBody_FixedWithAddAuthorizationBuilder()
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
    public async Task MultipleAddPolicyCalls_UsingExpressionBody_FixedWithAddAuthorizationBuilder()
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
    public async Task SingleAddPolicyCall_UsingBlockBody_FixedWithAddAuthorizationBuilder()
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
    public async Task MultipleAddPolicyCalls_UsingBlockBody_FixedWithAddAuthorizationBuilder()
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
    public async Task AuthorizationOptions_DefaultPolicyAssignment_ReplacedWithSetDefaultPolicyInvocation()
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
    public async Task AuthorizationOptions_FallbackPolicyAssignment_ReplacedWithSetFallbackPolicyInvocation()
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
    public async Task AuthorizationOptions_InvokeHandlersAfterFailureAssignment_ReplacedWithSetInvokeHandlersAfterFailureInvocation()
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
    public async Task AddAuthorization_IsTheLastCallInChain_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
           .WithLocation(0)
           .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

{|#0:builder.Services.AddRouting()
    .AddAuthorization(options =>
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

builder.Services.AddRouting()
    .AddAuthorizationBuilder()
    .AddPolicy(""AtLeast21"", policy =>
            policy.Requirements.Add(new MinimumAgeRequirement(21)));
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_IsNotTheLastCallInChain_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
})
.AddAuthentication();
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_DefaultPolicyAccess_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_DefaultPolicyAccess_SelfAssignment_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = options.DefaultPolicy;
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_FallbackPolicyAccess_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = options.FallbackPolicy;
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_FallbackPolicyAccess_SelfAssignment_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.FallbackPolicy;
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_InvokeHandlesAfterFailureAccess_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.InvokeHandlersAfterFailure = !options.InvokeHandlersAfterFailure;
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_InvokeHandlesAfterFailureAccess_SelfAssignment_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.InvokeHandlersAfterFailure = options.InvokeHandlersAfterFailure;
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_GetPolicyReference_NoDiagnostic()
    {
        var source = @"
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(""AtLeast21"", ((Func<string, AuthorizationPolicy?>)options.GetPolicy)(string.Empty)!);
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task AuthorizationOptions_GetPolicyInvocation_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(""AtLeast21"", options.GetPolicy(string.Empty)!);
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task ConfigureAction_IsNotAnonymousFunction_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(Helper.ConfigureAuthorization);

public static class Helper
{
    public static void ConfigureAuthorization(AuthorizationOptions options)
    {
        options.AddPolicy(""AtLeast21"", policy =>
        {
            policy.Requirements.Add(new MinimumAgeRequirement(21));
        });
    }
}
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task ConfigureAction_AuthorizationOptionsPassedToMethodCall_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options => Helper.ConfigureAuthorization(options));

public static class Helper
{
    public static void ConfigureAuthorization(AuthorizationOptions options)
    {
        options.AddPolicy(""AtLeast21"", policy =>
        {
            policy.Requirements.Add(new MinimumAgeRequirement(21));
        });
    }
}
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task ConfigureAction_ContainsOperationsNotRelatedToAuthorizationOptions_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    var value = 1 + 1;
    options.AddPolicy(""AtLeast21"", policy =>
    {
        policy.Requirements.Add(new MinimumAgeRequirement(21));
    });
});
";

        await VerifyNoCodeFix(source);
    }

    [Fact]
    public async Task NestedAddAuthorization_UsingBlockBody_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
           .WithLocation(0)
           .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        {|#0:services.AddAuthorization(options =>
        {
             options.AddPolicy(""AtLeast21"", policy =>
            {
                policy.Requirements.Add(new MinimumAgeRequirement(21));
            });
        })|};
    });
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(""AtLeast21"", policy =>
            {
                policy.Requirements.Add(new MinimumAgeRequirement(21));
            });
    });
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task NestedAddAuthorization_UsingExpressionBody_FixedWithAddAuthorizationBuilder()
    {
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseAddAuthorizationBuilder)
           .WithLocation(0)
           .WithMessage(Resources.Analyzer_UseAddAuthorizationBuilder_Message);

        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        {|#0:services.AddAuthorization(options =>
            options.AddPolicy(""AtLeast21"", policy =>
                policy.Requirements.Add(new MinimumAgeRequirement(21))))|};
    });
";

        var fixedSource = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(""AtLeast21"", policy =>
                policy.Requirements.Add(new MinimumAgeRequirement(21)));
    });
";

        await VerifyCodeFix(source, new[] { diagnostic }, fixedSource);
    }

    [Fact]
    public async Task AddAuthorization_CallAssignedToVariable_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services.AddAuthorization(options =>
    options.AddPolicy(""AtLeast21"", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21))));
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
