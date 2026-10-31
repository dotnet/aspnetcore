// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

/// <summary>
/// This helper class is used to check that query string parameters are as expected.
/// </summary>
internal class TestSettings
{
    private readonly Action<OpenIdConnectOptions> _configureOptions;
    private OpenIdConnectOptions _options;

    public TestSettings() : this(configure: null)
    {
    }

    public TestSettings(Action<OpenIdConnectOptions> configure, HttpMessageHandler backchannel = null)
    {
        if (backchannel == null)
        {
            backchannel = new MockBackchannel();
        }
        _configureOptions = o =>
        {
            configure?.Invoke(o);
            _options = o;
            _options.BackchannelHttpHandler = backchannel;
        };
    }

    public UrlEncoder Encoder => UrlEncoder.Default;

    public string ExpectedState { get; set; }

    public TestServer CreateTestServer(AuthenticationProperties properties = null, Func<HttpContext, Task> handler = null) => TestServerBuilder.CreateServer(_configureOptions, handler: handler, properties: properties);

    public IDictionary<string, string> ValidateChallengeFormPost(string responseBody, params string[] parametersToValidate)
    {
        IDictionary<string, string> formInputs = null;
        var errors = new List<string>();
        var xdoc = XDocument.Parse(responseBody.Replace("doctype", "DOCTYPE"));
        var forms = xdoc.Descendants("form");
        if (forms.Count() != 1)
        {
            errors.Add("Only one form element is expected in response body.");
        }
        else
        {
            formInputs = forms.Single()
                              .Elements("input")
                              .ToDictionary(elem => elem.Attribute("name").Value,
                                            elem => elem.Attribute("value").Value);

            ValidateParameters(formInputs, parametersToValidate, errors, htmlEncoded: false);
        }

        if (errors.Any())
        {
            var buf = new StringBuilder();
            buf.AppendLine("The challenge form post is not valid.");
            // buf.AppendLine();

            foreach (var error in errors)
            {
                buf.AppendLine(error);
            }

            Debug.WriteLine(buf.ToString());
            Assert.Fail(buf.ToString());
        }

        return formInputs;
    }

    public IDictionary<string, string> ValidateSignoutFormPost(TestTransaction transaction, params string[] parametersToValidate)
    {
        IDictionary<string, string> formInputs = null;
        var errors = new List<string>();
        var xdoc = XDocument.Parse(transaction.ResponseText.Replace("doctype", "DOCTYPE"));
        var forms = xdoc.Descendants("form");
        if (forms.Count() != 1)
        {
            errors.Add("Only one form element is expected in response body.");
        }
        else
        {
            formInputs = forms.Single()
                              .Elements("input")
                              .ToDictionary(elem => elem.Attribute("name").Value,
                                            elem => elem.Attribute("value").Value);

            ValidateParameters(formInputs, parametersToValidate, errors, htmlEncoded: false);
        }

        if (errors.Any())
        {
            var buf = new StringBuilder();
            buf.AppendLine("The signout form post is not valid.");
            // buf.AppendLine();

            foreach (var error in errors)
            {
                buf.AppendLine(error);
            }

            Debug.WriteLine(buf.ToString());
            Assert.Fail(buf.ToString());
        }

        return formInputs;
    }

    public IDictionary<string, string> ValidateChallengeRedirect(Uri redirectUri, params string[] parametersToValidate) =>
        ValidateRedirectCore(redirectUri, OpenIdConnectRequestType.Authentication, parametersToValidate);

    public IDictionary<string, string> ValidateSignoutRedirect(Uri redirectUri, params string[] parametersToValidate) =>
        ValidateRedirectCore(redirectUri, OpenIdConnectRequestType.Logout, parametersToValidate);

    private IDictionary<string, string> ValidateRedirectCore(Uri redirectUri, OpenIdConnectRequestType requestType, string[] parametersToValidate)
    {
        var errors = new List<string>();

        // Validate the authority
        ValidateExpectedAuthority(redirectUri.AbsoluteUri, errors, requestType);

        // Convert query to dictionary
        var queryDict = string.IsNullOrEmpty(redirectUri.Query) ?
            new Dictionary<string, string>() :
            redirectUri.Query.TrimStart('?').Split('&').Select(part => part.Split('=')).ToDictionary(parts => parts[0], parts => parts[1]);

        // Validate the query string parameters
        ValidateParameters(queryDict, parametersToValidate, errors, htmlEncoded: true);

        if (errors.Any())
        {
            var buf = new StringBuilder();
            buf.AppendLine("The redirect uri is not valid.");
            buf.AppendLine(redirectUri.AbsoluteUri);

            foreach (var error in errors)
            {
                buf.AppendLine(error);
            }

            Debug.WriteLine(buf.ToString());
            Assert.Fail(buf.ToString());
        }

        return queryDict;
    }

    private void ValidateParameters(
        IDictionary<string, string> actualValues,
        IEnumerable<string> parametersToValidate,
        ICollection<string> errors,
        bool htmlEncoded)
    {
        foreach (var paramToValidate in parametersToValidate)
        {
            switch (paramToValidate)
            {
                case OpenIdConnectParameterNames.ClientId:
                    ValidateClientId(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.ResponseType:
                    ValidateResponseType(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.ResponseMode:
                    ValidateResponseMode(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.Scope:
                    ValidateScope(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.RedirectUri:
                    ValidateRedirectUri(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.Resource:
                    ValidateResource(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.State:
                    ValidateState(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.SkuTelemetry:
                    ValidateSkuTelemetry(actualValues, errors);
                    break;
                case OpenIdConnectParameterNames.VersionTelemetry:
                    ValidateVersionTelemetry(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.PostLogoutRedirectUri:
                    ValidatePostLogoutRedirectUri(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.MaxAge:
                    ValidateMaxAge(actualValues, errors, htmlEncoded);
                    break;
                case OpenIdConnectParameterNames.Prompt:
                    ValidatePrompt(actualValues, errors, htmlEncoded);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown parameter \"{paramToValidate}\".");
            }
        }
    }

    private void ValidateExpectedAuthority(string absoluteUri, ICollection<string> errors, OpenIdConnectRequestType requestType)
    {
        string expectedAuthority;
        switch (requestType)
        {
            case OpenIdConnectRequestType.Token:
                expectedAuthority = _options.Configuration?.TokenEndpoint ?? _options.Authority + @"/oauth2/token";
                break;
            case OpenIdConnectRequestType.Logout:
                expectedAuthority = _options.Configuration?.EndSessionEndpoint ?? _options.Authority + @"/oauth2/logout";
                break;
            default:
                expectedAuthority = _options.Configuration?.AuthorizationEndpoint ?? _options.Authority + @"/oauth2/authorize";
                break;
        }

        if (!absoluteUri.StartsWith(expectedAuthority, StringComparison.Ordinal))
        {
            errors.Add($"ExpectedAuthority: {expectedAuthority}");
        }
    }

    private void ValidateClientId(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.ClientId, _options.ClientId, actualParams, errors, htmlEncoded);

    private void ValidateResponseType(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.ResponseType, _options.ResponseType, actualParams, errors, htmlEncoded);

    private void ValidateResponseMode(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.ResponseMode, _options.ResponseMode, actualParams, errors, htmlEncoded);

    private void ValidateScope(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.Scope, string.Join(" ", _options.Scope), actualParams, errors, htmlEncoded);

    private void ValidateRedirectUri(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.RedirectUri, TestServerBuilder.TestHost + _options.CallbackPath, actualParams, errors, htmlEncoded);

    private void ValidateResource(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.RedirectUri, _options.Resource, actualParams, errors, htmlEncoded);

    private void ValidateState(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.State, ExpectedState, actualParams, errors, htmlEncoded);

    private static void ValidateSkuTelemetry(IDictionary<string, string> actualParams, ICollection<string> errors)
    {
        if (!actualParams.ContainsKey(OpenIdConnectParameterNames.SkuTelemetry))
        {
            errors.Add($"Parameter {OpenIdConnectParameterNames.SkuTelemetry} is missing");
        }
    }

    private void ValidateVersionTelemetry(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.VersionTelemetry, typeof(OpenIdConnectMessage).GetTypeInfo().Assembly.GetName().Version.ToString(), actualParams, errors, htmlEncoded);

    private void ValidatePostLogoutRedirectUri(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.PostLogoutRedirectUri, "https://example.com/signout-callback-oidc", actualParams, errors, htmlEncoded);

    private void ValidateMaxAge(IDictionary<string, string> actualQuery, ICollection<string> errors, bool htmlEncoded)
    {
        if (_options.MaxAge.HasValue)
        {
            Assert.Equal(TimeSpan.FromMinutes(20), _options.MaxAge.Value);
            string expectedMaxAge = "1200";
            ValidateParameter(OpenIdConnectParameterNames.MaxAge, expectedMaxAge, actualQuery, errors, htmlEncoded);
        }
        else if (actualQuery.ContainsKey(OpenIdConnectParameterNames.MaxAge))
        {
            errors.Add($"Parameter {OpenIdConnectParameterNames.MaxAge} is present but it should be absent");
        }
    }

    private void ValidatePrompt(IDictionary<string, string> actualParams, ICollection<string> errors, bool htmlEncoded) =>
        ValidateParameter(OpenIdConnectParameterNames.Prompt, _options.Prompt, actualParams, errors, htmlEncoded);

    private void ValidateParameter(
        string parameterName,
        string expectedValue,
        IDictionary<string, string> actualParams,
        ICollection<string> errors,
        bool htmlEncoded)
    {
        string actualValue;
        if (actualParams.TryGetValue(parameterName, out actualValue))
        {
            if (htmlEncoded)
            {
                expectedValue = Encoder.Encode(expectedValue);
            }

            if (actualValue != expectedValue)
            {
                errors.Add($"Parameter {parameterName}'s expected value is '{expectedValue}' but its actual value is '{actualValue}'");
            }
        }
        else
        {
            errors.Add($"Parameter {parameterName} is missing");
        }
    }

    private class MockBackchannel : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri.AbsoluteUri.Equals("https://login.microsoftonline.com/common/.well-known/openid-configuration"))
            {
                return await ReturnResource("wellknownconfig.json");
            }
            if (request.RequestUri.AbsoluteUri.Equals("https://login.microsoftonline.com/common/discovery/keys"))
            {
                return await ReturnResource("wellknownkeys.json");
            }

            throw new NotImplementedException();
        }

        private async Task<HttpResponseMessage> ReturnResource(string resource)
        {
            var resourceName = "Microsoft.AspNetCore.Authentication.Test.OpenIdConnect." + resource;
            using (var stream = typeof(MockBackchannel).Assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var body = await reader.ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                return new HttpResponseMessage()
                {
                    Content = content,
                };
            }
        }
    }
}
