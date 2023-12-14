// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.Extensions.Http;

/// <summary>
/// A <see cref="DelegatingHandler"/> implementation that executes request processing surrounded by a <see cref="Policy"/>.
/// </summary>
/// <remarks>
/// <para>
/// This message handler implementation supports the use of policies provided by the Polly library for
/// transient-fault-handling and resiliency.
/// </para>
/// <para>
/// The documentation provided here is focused guidance for using Polly together with the <see cref="IHttpClientFactory"/>.
/// See the Polly project and its documentation (<see href="https://github.com/app-vnext/Polly"/>) for authoritative information on Polly.
/// </para>
/// <para>
/// The extension methods on <see cref="PollyHttpClientBuilderExtensions"/> are designed as a convenient and correct
/// way to create a <see cref="PolicyHttpMessageHandler"/>.
/// </para>
/// <para>
/// The <see cref="PollyHttpClientBuilderExtensions.AddPolicyHandler(IHttpClientBuilder, IAsyncPolicy{HttpResponseMessage})"/>
/// method supports the creation of a <see cref="PolicyHttpMessageHandler"/> for any kind of policy. This includes
/// non-reactive policies, such as Timeout or Cache, which don't require the underlying request to fail first.
/// </para>
/// <para>
/// <see cref="PolicyHttpMessageHandler"/> and the <see cref="PollyHttpClientBuilderExtensions"/> convenience methods
/// only accept the generic <see cref="IAsyncPolicy{HttpResponseMessage}"/>. Generic policy instances can be created
/// by using the generic methods on <see cref="Policy"/> such as <see cref="Policy.TimeoutAsync{TResult}(int)"/>.
/// </para>
/// <para>
/// To adapt an existing non-generic <see cref="IAsyncPolicy"/>, use code like the following:
/// <example>
/// Converting a non-generic <c>IAsyncPolicy policy</c> to <see cref="IAsyncPolicy{HttpResponseMessage}"/>.
/// <code>
/// policy.AsAsyncPolicy&lt;HttpResponseMessage&gt;()
/// </code>
/// </example>
/// </para>
/// <para>
/// The <see cref="PollyHttpClientBuilderExtensions.AddTransientHttpErrorPolicy(IHttpClientBuilder, Func{PolicyBuilder{HttpResponseMessage}, IAsyncPolicy{HttpResponseMessage}})"/>
/// method is an opinionated convenience method that supports the application of a policy for requests that fail due
/// to a connection failure or server error (5XX HTTP status code). This kind of method supports only reactive policies
/// such as Retry, Circuit-Breaker or Fallback. This method is only provided for convenience; we recommend creating
/// your own policies as needed if this does not meet your requirements.
/// </para>
/// <para>
/// Take care when using policies such as Retry or Timeout together as HttpClient provides its own timeout via
/// <see cref="HttpClient.Timeout"/>.  When combining Retry and Timeout, <see cref="HttpClient.Timeout"/> will act as a
/// timeout across all tries; a Polly Timeout policy can be configured after a Retry policy in the configuration sequence,
/// to provide a timeout-per-try.
/// </para>
/// <para>
/// All policies provided by Polly are designed to be efficient when used in a long-lived way. Certain policies such as the
/// Bulkhead and Circuit-Breaker maintain state and should be scoped across calls you wish to share the Bulkhead or Circuit-Breaker state.
/// Take care to ensure the correct lifetimes when using policies and message handlers together in custom scenarios. The extension
/// methods provided by <see cref="PollyHttpClientBuilderExtensions"/> are designed to assign a long lifetime to policies
/// and ensure that they can be used when the handler rotation feature is active.
/// </para>
/// <para>
/// The <see cref="PolicyHttpMessageHandler"/> will attach a context to the <see cref="HttpRequestMessage"/> prior
/// to executing a <see cref="Policy"/>, if one does not already exist. The <see cref="Context"/> will be provided
/// to the policy for use inside the <see cref="Policy"/> and in other message handlers.
/// </para>
/// </remarks>
public class PolicyHttpMessageHandler : DelegatingHandler
{
    private const string PriorResponseKey = "PolicyHttpMessageHandler.PriorResponse";
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;
    private readonly Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> _policySelector;

    /// <summary>
    /// Creates a new <see cref="PolicyHttpMessageHandler"/>.
    /// </summary>
    /// <param name="policy">The policy.</param>
    public PolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        _policy = policy;
    }

    /// <summary>
    /// Creates a new <see cref="PolicyHttpMessageHandler"/>.
    /// </summary>
    /// <param name="policySelector">A function which can select the desired policy for a given <see cref="HttpRequestMessage"/>.</param>
    public PolicyHttpMessageHandler(Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
    {
        if (policySelector == null)
        {
            throw new ArgumentNullException(nameof(policySelector));
        }

        _policySelector = policySelector;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Guarantee the existence of a context for every policy execution, but only create a new one if needed. This
        // allows later handlers to flow state if desired.
        var cleanUpContext = false;
        var context = request.GetPolicyExecutionContext();
        if (context == null)
        {
            context = new Context();
            request.SetPolicyExecutionContext(context);
            cleanUpContext = true;
        }

        HttpResponseMessage response;
        try
        {
            var policy = _policy ?? SelectPolicy(request);
            response = await policy.ExecuteAsync((c, ct) => SendCoreAsync(request, c, ct), context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (cleanUpContext)
            {
                request.SetPolicyExecutionContext(null);
            }
        }

        return response;
    }

    /// <summary>
    /// Called inside the execution of the <see cref="Policy"/> to perform request processing.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="context">The <see cref="Context"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Returns a <see cref="Task{HttpResponseMessage}"/> that will yield a response when completed.</returns>
    protected virtual async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, Context context, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (request.Properties.TryGetValue(PriorResponseKey, out var priorResult) && priorResult is IDisposable disposable)
        {
            // This is a retry, dispose the prior response to free up the connection.
            request.Properties.Remove(PriorResponseKey);
            disposable.Dispose();
        }

        var result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        request.Properties[PriorResponseKey] = result;

        return result;
    }

    private IAsyncPolicy<HttpResponseMessage> SelectPolicy(HttpRequestMessage request)
    {
        var policy = _policySelector(request);
        if (policy == null)
        {
            var message = Resources.FormatPolicyHttpMessageHandler_PolicySelector_ReturnedNull(
                "policySelector",
                "Policy.NoOpAsync<HttpResponseMessage>()");
            throw new InvalidOperationException(message);
        }

        return policy;
    }
}
