// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// Default implementation of <see cref="ICorsService"/>.
/// </summary>
public class CorsService : ICorsService
{
    private readonly CorsOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of the <see cref="CorsService"/>.
    /// </summary>
    /// <param name="options">The option model representing <see cref="CorsOptions"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public CorsService(IOptions<CorsOptions> options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options.Value;
        _logger = loggerFactory.CreateLogger<CorsService>();
    }

    /// <summary>
    /// Looks up a policy using the <paramref name="policyName"/> and then evaluates the policy using the passed in
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="policyName"></param>
    /// <returns>A <see cref="CorsResult"/> which contains the result of policy evaluation and can be
    /// used by the caller to set appropriate response headers.</returns>
    public CorsResult EvaluatePolicy(HttpContext context, string policyName)
    {
        ArgumentNullException.ThrowIfNull(context);

        var policy = _options.GetPolicy(policyName);
        if (policy is null)
        {
            throw new InvalidOperationException(Resources.FormatPolicyNotFound(policyName));
        }

        return EvaluatePolicy(context, policy);
    }

    /// <inheritdoc />
    public CorsResult EvaluatePolicy(HttpContext context, CorsPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.AllowAnyOrigin && policy.SupportsCredentials)
        {
            throw new ArgumentException(Resources.InsecureConfiguration, nameof(policy));
        }

        var requestHeaders = context.Request.Headers;
        var origin = requestHeaders.Origin;

        var isOptionsRequest = HttpMethods.IsOptions(context.Request.Method);
        var isPreflightRequest = isOptionsRequest && requestHeaders.ContainsKey(CorsConstants.AccessControlRequestMethod);

        if (isOptionsRequest && !isPreflightRequest)
        {
            _logger.IsNotPreflightRequest();
        }

        var corsResult = new CorsResult
        {
            IsPreflightRequest = isPreflightRequest,
            IsOriginAllowed = IsOriginAllowed(policy, origin),
        };

        if (isPreflightRequest)
        {
            EvaluatePreflightRequest(context, policy, corsResult);
        }
        else
        {
            EvaluateRequest(context, policy, corsResult);
        }

        return corsResult;
    }

    private static void PopulateResult(HttpContext context, CorsPolicy policy, CorsResult result)
    {
        var headers = context.Request.Headers;
        if (policy.AllowAnyOrigin)
        {
            result.AllowedOrigin = CorsConstants.AnyOrigin;
            result.VaryByOrigin = policy.SupportsCredentials;
        }
        else
        {
            var origin = headers.Origin;
            result.AllowedOrigin = origin;
            result.VaryByOrigin = policy.Origins.Count > 1 || !policy.IsDefaultIsOriginAllowed;
        }

        result.SupportsCredentials = policy.SupportsCredentials;
        result.PreflightMaxAge = policy.PreflightMaxAge;

        // https://fetch.spec.whatwg.org/#http-new-header-syntax
        AddHeaderValues(result.AllowedExposedHeaders, policy.ExposedHeaders);

        var allowedMethods = policy.AllowAnyMethod ?
            new[] { result.IsPreflightRequest ? headers.AccessControlRequestMethod.ToString() : context.Request.Method } :
            policy.Methods;
        AddHeaderValues(result.AllowedMethods, allowedMethods);

        var allowedHeaders = policy.AllowAnyHeader ?
            headers.GetCommaSeparatedValues(CorsConstants.AccessControlRequestHeaders) :
            policy.Headers;
        AddHeaderValues(result.AllowedHeaders, allowedHeaders);
    }

    /// <summary>
    /// Evaluate a request using the specified policy. The result is set on the specified <see cref="CorsResult"/> instance.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="policy">The <see cref="CorsPolicy"/> to evaluate.</param>
    /// <param name="result">The <see cref="CorsResult"/> to set the result on.</param>
    public virtual void EvaluateRequest(HttpContext context, CorsPolicy policy, CorsResult result)
    {
        PopulateResult(context, policy, result);
    }

    /// <summary>
    /// Evaluate a preflight request using the specified policy. The result is set on the specified <see cref="CorsResult"/> instance.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="policy">The <see cref="CorsPolicy"/> to evaluate.</param>
    /// <param name="result">The <see cref="CorsResult"/> to set the result on.</param>
    public virtual void EvaluatePreflightRequest(HttpContext context, CorsPolicy policy, CorsResult result)
    {
        PopulateResult(context, policy, result);
    }

    /// <inheritdoc />
    public virtual void ApplyResult(CorsResult result, HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(response);

        if (!result.IsOriginAllowed)
        {
            // In case a server does not wish to participate in the CORS protocol, its HTTP response to the
            // CORS or CORS-preflight request must not include any of the above headers.
            return;
        }

        var headers = response.Headers;
        headers.AccessControlAllowOrigin = result.AllowedOrigin;

        if (result.SupportsCredentials)
        {
            headers.AccessControlAllowCredentials = "true";
        }

        if (result.IsPreflightRequest)
        {
            _logger.IsPreflightRequest();

            // An HTTP response to a CORS-preflight request can include the following headers:
            // `Access-Control-Allow-Methods`, `Access-Control-Allow-Headers`, `Access-Control-Max-Age`
            if (result.AllowedHeaders.Count > 0)
            {
                headers.SetCommaSeparatedValues(CorsConstants.AccessControlAllowHeaders, result.AllowedHeaders.ToArray());
            }

            if (result.AllowedMethods.Count > 0)
            {
                headers.SetCommaSeparatedValues(CorsConstants.AccessControlAllowMethods, result.AllowedMethods.ToArray());
            }

            if (result.PreflightMaxAge.HasValue)
            {
                headers.AccessControlMaxAge = result.PreflightMaxAge.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
        }
        else
        {
            // An HTTP response to a CORS request that is not a CORS-preflight request can also include the following header:
            // `Access-Control-Expose-Headers`
            if (result.AllowedExposedHeaders.Count > 0)
            {
                headers.SetCommaSeparatedValues(CorsConstants.AccessControlExposeHeaders, result.AllowedExposedHeaders.ToArray());
            }
        }

        if (result.VaryByOrigin)
        {
            headers.Append(HeaderNames.Vary, "Origin");
        }
    }

    private static void AddHeaderValues(IList<string> target, IList<string> headerValues)
    {
        if (headerValues == null)
        {
            return;
        }

        for (var i = 0; i < headerValues.Count; i++)
        {
            target.Add(headerValues[i]);
        }
    }

    private bool IsOriginAllowed(CorsPolicy policy, StringValues origin)
    {
        if (StringValues.IsNullOrEmpty(origin))
        {
            _logger.RequestDoesNotHaveOriginHeader();
            return false;
        }

        var originString = origin.ToString();
        _logger.RequestHasOriginHeader(originString);
        if (policy.AllowAnyOrigin || policy.IsOriginAllowed(originString))
        {
            _logger.PolicySuccess();
            return true;
        }
        _logger.PolicyFailure();
        _logger.OriginNotAllowed(originString);
        return false;
    }
}
