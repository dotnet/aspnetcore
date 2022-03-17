// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions methods for configuring <see cref="PolicyHttpMessageHandler"/> message handlers as part of
/// and <see cref="HttpClient"/> message handler pipeline.
/// </summary>
public static class PollyHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with the provided
    /// <see cref="IAsyncPolicy{HttpResponseMessage}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policy">The <see cref="IAsyncPolicy{HttpResponseMessage}"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddPolicyHandler(this IHttpClientBuilder builder, IAsyncPolicy<HttpResponseMessage> policy)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policy));
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
    /// by the <paramref name="policySelector"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policySelector">
    /// Selects an <see cref="IAsyncPolicy{HttpResponseMessage}"/> to apply to the current request.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddPolicyHandler(
        this IHttpClientBuilder builder,
        Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (policySelector == null)
        {
            throw new ArgumentNullException(nameof(policySelector));
        }

        builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policySelector));
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
    /// by the <paramref name="policySelector"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policySelector">
    /// Selects an <see cref="IAsyncPolicy{HttpResponseMessage}"/> to apply to the current request.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddPolicyHandler(
        this IHttpClientBuilder builder,
        Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (policySelector == null)
        {
            throw new ArgumentNullException(nameof(policySelector));
        }

        builder.AddHttpMessageHandler((services) =>
        {
            return new PolicyHttpMessageHandler((request) => policySelector(services, request));
        });
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
    /// by the <see cref="IReadOnlyPolicyRegistry{String}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policyKey">
    /// The key used to resolve a policy from the <see cref="IReadOnlyPolicyRegistry{String}"/>.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddPolicyHandlerFromRegistry(this IHttpClientBuilder builder, string policyKey)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (policyKey == null)
        {
            throw new ArgumentNullException(nameof(policyKey));
        }

        builder.AddHttpMessageHandler((services) =>
        {
            var registry = services.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

            var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>(policyKey);

            return new PolicyHttpMessageHandler(policy);
        });
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
    /// by the <see cref="IReadOnlyPolicyRegistry{String}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policySelector">
    /// Selects an <see cref="IAsyncPolicy{HttpResponseMessage}"/> to apply to the current request.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddPolicyHandlerFromRegistry(
        this IHttpClientBuilder builder,
        Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (policySelector == null)
        {
            throw new ArgumentNullException(nameof(policySelector));
        }

        builder.AddHttpMessageHandler((services) =>
        {
            var registry = services.GetRequiredService<IReadOnlyPolicyRegistry<string>>();
            return new PolicyHttpMessageHandler((request) => policySelector(registry, request));
        });
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a <see cref="Policy"/>
    /// created by executing the provided configuration delegate. The policy builder will be preconfigured to trigger
    /// application of the policy for requests that fail with conditions that indicate a transient failure.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="configurePolicy">A delegate used to create a <see cref="IAsyncPolicy{HttpResponseMessage}"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// <para>
    /// The <see cref="PolicyBuilder{HttpResponseMessage}"/> provided to <paramref name="configurePolicy"/> has been
    /// preconfigured errors to handle errors in the following categories:
    /// <list type="bullet">
    /// <item><description>Network failures (as <see cref="HttpRequestException"/>)</description></item>
    /// <item><description>HTTP 5XX status codes (server errors)</description></item>
    /// <item><description>HTTP 408 status code (request timeout)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The policy created by <paramref name="configurePolicy"/> will be cached indefinitely per named client. Policies
    /// are generally designed to act as singletons, and can be shared when appropriate. To share a policy across multiple
    /// named clients, first create the policy and then pass it to multiple calls to 
    /// <see cref="AddPolicyHandler(IHttpClientBuilder, IAsyncPolicy{HttpResponseMessage})"/> as desired.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddTransientHttpErrorPolicy(
        this IHttpClientBuilder builder,
        Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configurePolicy == null)
        {
            throw new ArgumentNullException(nameof(configurePolicy));
        }

        var policyBuilder = HttpPolicyExtensions.HandleTransientHttpError();

        // Important - cache policy instances so that they are singletons per handler.
        var policy = configurePolicy(policyBuilder);

        builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policy));
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
    /// by executing provided key selection logic <paramref name="keySelector"/> and <paramref name="policyFactory"/>
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policyFactory">Selects an <see cref="IAsyncPolicy{HttpResponseMessage}"/> to apply to the current request based on key selection.</param>
    /// <param name="keySelector">A delegate used to generate a policy key based on the <see cref="HttpRequestMessage"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// Key generated by <paramref name="policyFactory"/> is first used to lookup existing policies from IPolicyRegistry. If policy does not exist in the registry, create a new policy with <paramref name="policyFactory"/> and add it in IPolicyRegistry.
    /// </para>
    /// <para>
    /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddPolicyHandler(this IHttpClientBuilder builder, Func<IServiceProvider, HttpRequestMessage, string, IAsyncPolicy<HttpResponseMessage>> policyFactory, Func<HttpRequestMessage, string> keySelector)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        if (policyFactory == null)
        {
            throw new ArgumentNullException(nameof(policyFactory));
        }

        builder.AddHttpMessageHandler((services) =>
        {
            var registry = services.GetRequiredService<IPolicyRegistry<string>>();
            return new PolicyHttpMessageHandler((request) =>
            {
                var key = keySelector(request);

                if (registry.TryGet<IAsyncPolicy<HttpResponseMessage>>(key, out var policy))
                {
                    return policy;
                }

                var newPolicy = policyFactory(services, request, key);
                registry[key] = newPolicy;
                return newPolicy;
            });
        });
        return builder;
    }
}
