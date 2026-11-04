// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CustomAuthorizationFailureResponse.Authentication;
using CustomAuthorizationFailureResponse.Authorization;
using CustomAuthorizationFailureResponse.Authorization.Handlers;
using CustomAuthorizationFailureResponse.Authorization.Requirements;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomAuthorizationFailureResponse;

public class Startup
{
    public const string CustomForbiddenMessage = "Some info about the error";

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services
            .AddAuthentication(SampleAuthenticationSchemes.CustomScheme)
            .AddScheme<AuthenticationSchemeOptions, SampleAuthenticationHandler>(SampleAuthenticationSchemes.CustomScheme, o => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(SamplePolicyNames.CustomPolicy, policy =>
                policy.AddRequirements(new SampleRequirement()));

            options.AddPolicy(SamplePolicyNames.FailureReasonPolicy, policy =>
                policy.AddRequirements(new SampleFailReasonRequirement()));

            options.AddPolicy(SamplePolicyNames.CustomPolicyWithCustomForbiddenMessage, policy =>
                policy.AddRequirements(new SampleWithCustomMessageRequirement()));
        });

        services.AddTransient<IAuthorizationHandler, SampleRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, SampleWithCustomMessageRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, SampleWithFailureReasonRequirementHandler>();
        services.AddTransient<IAuthorizationMiddlewareResultHandler, SampleAuthorizationMiddlewareResultHandler>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }
}
