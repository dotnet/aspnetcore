// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(13, LogLevel.Debug, "Updating configuration", EventName = "UpdatingConfiguration")]
    public static partial void UpdatingConfiguration(this ILogger logger);

    [LoggerMessage(18, LogLevel.Debug, "Exception of type 'SecurityTokenSignatureKeyNotFoundException' thrown, Options.ConfigurationManager.RequestRefresh() called.", EventName = "ConfigurationManagerRequestRefreshCalled")]
    public static partial void ConfigurationManagerRequestRefreshCalled(this ILogger logger);

    [LoggerMessage(27, LogLevel.Trace, "Authorization code received.", EventName = "AuthorizationCodeReceived")]
    public static partial void AuthorizationCodeReceived(this ILogger logger);

    [LoggerMessage(30, LogLevel.Trace, "Token response received.", EventName = "TokenResponseReceived")]
    public static partial void TokenResponseReceived(this ILogger logger);

    [LoggerMessage(21, LogLevel.Debug, "Received 'id_token'", EventName = "ReceivedIdToken")]
    public static partial void ReceivedIdToken(this ILogger logger);

    [LoggerMessage(19, LogLevel.Debug, "Redeeming code for tokens.", EventName = "RedeemingCodeForTokens")]
    public static partial void RedeemingCodeForTokens(this ILogger logger);

    [LoggerMessage(15, LogLevel.Debug, "TokenValidated.HandledResponse", EventName = "TokenValidatedHandledResponse")]
    public static partial void TokenValidatedHandledResponse(this ILogger logger);

    [LoggerMessage(16, LogLevel.Debug, "TokenValidated.Skipped", EventName = "TokenValidatedSkipped")]
    public static partial void TokenValidatedSkipped(this ILogger logger);

    [LoggerMessage(28, LogLevel.Debug, "AuthorizationCodeReceivedContext.HandledResponse", EventName = "AuthorizationCodeReceivedContextHandledResponse")]
    public static partial void AuthorizationCodeReceivedContextHandledResponse(this ILogger logger);

    [LoggerMessage(29, LogLevel.Debug, "AuthorizationCodeReceivedContext.Skipped", EventName = "AuthorizationCodeReceivedContextSkipped")]
    public static partial void AuthorizationCodeReceivedContextSkipped(this ILogger logger);

    [LoggerMessage(31, LogLevel.Debug, "TokenResponseReceived.HandledResponse", EventName = "TokenResponseReceivedHandledResponse")]
    public static partial void TokenResponseReceivedHandledResponse(this ILogger logger);

    [LoggerMessage(32, LogLevel.Debug, "TokenResponseReceived.Skipped", EventName = "TokenResponseReceivedSkipped")]
    public static partial void TokenResponseReceivedSkipped(this ILogger logger);

    [LoggerMessage(38, LogLevel.Debug, "AuthenticationFailedContext.HandledResponse", EventName = "AuthenticationFailedContextHandledResponse")]
    public static partial void AuthenticationFailedContextHandledResponse(this ILogger logger);

    [LoggerMessage(39, LogLevel.Debug, "AuthenticationFailedContext.Skipped", EventName = "AuthenticationFailedContextSkipped")]
    public static partial void AuthenticationFailedContextSkipped(this ILogger logger);

    [LoggerMessage(24, LogLevel.Trace, "MessageReceived: '{RedirectUrl}'.", EventName = "MessageReceived")]
    public static partial void MessageReceived(this ILogger logger, string redirectUrl);

    [LoggerMessage(25, LogLevel.Debug, "MessageReceivedContext.HandledResponse", EventName = "MessageReceivedContextHandledResponse")]
    public static partial void MessageReceivedContextHandledResponse(this ILogger logger);

    [LoggerMessage(26, LogLevel.Debug, "MessageReceivedContext.Skipped", EventName = "MessageReceivedContextSkipped")]
    public static partial void MessageReceivedContextSkipped(this ILogger logger);

    [LoggerMessage(1, LogLevel.Debug, "RedirectToIdentityProviderForSignOut.HandledResponse", EventName = "RedirectToIdentityProviderForSignOutHandledResponse")]
    public static partial void RedirectToIdentityProviderForSignOutHandledResponse(this ILogger logger);

    [LoggerMessage(6, LogLevel.Debug, "RedirectToIdentityProvider.HandledResponse", EventName = "RedirectToIdentityProviderHandledResponse")]
    public static partial void RedirectToIdentityProviderHandledResponse(this ILogger logger);

    [LoggerMessage(50, LogLevel.Debug, "RedirectToSignedOutRedirectUri.HandledResponse", EventName = "SignOutCallbackRedirectHandledResponse")]
    public static partial void SignOutCallbackRedirectHandledResponse(this ILogger logger);

    [LoggerMessage(51, LogLevel.Debug, "RedirectToSignedOutRedirectUri.Skipped", EventName = "SignOutCallbackRedirectSkipped")]
    public static partial void SignOutCallbackRedirectSkipped(this ILogger logger);

    [LoggerMessage(36, LogLevel.Debug, "The UserInformationReceived event returned Handled.", EventName = "UserInformationReceivedHandledResponse")]
    public static partial void UserInformationReceivedHandledResponse(this ILogger logger);

    [LoggerMessage(37, LogLevel.Debug, "The UserInformationReceived event returned Skipped.", EventName = "UserInformationReceivedSkipped")]
    public static partial void UserInformationReceivedSkipped(this ILogger logger);

    [LoggerMessage(57, LogLevel.Debug, "The PushAuthorization event handled client authentication", EventName = "PushAuthorizationHandledClientAuthentication")]
    public static partial void PushAuthorizationHandledClientAuthentication(this ILogger logger);

    [LoggerMessage(58, LogLevel.Debug, "The PushAuthorization event handled pushing authorization", EventName = "PushAuthorizationHandledPush")]
    public static partial void PushAuthorizationHandledPush(this ILogger logger);

    [LoggerMessage(59, LogLevel.Debug, "The PushAuthorization event skipped pushing authorization", EventName = "PushAuthorizationSkippedPush")]
    public static partial void PushAuthorizationSkippedPush(this ILogger logger);

    [LoggerMessage(3, LogLevel.Warning, "The query string for Logout is not a well-formed URI. Redirect URI: '{RedirectUrl}'.", EventName = "InvalidLogoutQueryStringRedirectUrl")]
    public static partial void InvalidLogoutQueryStringRedirectUrl(this ILogger logger, string redirectUrl);

    [LoggerMessage(10, LogLevel.Debug, "message.State is null or empty.", EventName = "NullOrEmptyAuthorizationResponseState")]
    public static partial void NullOrEmptyAuthorizationResponseState(this ILogger logger);

    [LoggerMessage(11, LogLevel.Debug, "Unable to read the message.State.", EventName = "UnableToReadAuthorizationResponseState")]
    public static partial void UnableToReadAuthorizationResponseState(this ILogger logger);

    [LoggerMessage(12, LogLevel.Error, "Message contains error: '{Error}', error_description: '{ErrorDescription}', error_uri: '{ErrorUri}'.", EventName = "ResponseError")]
    public static partial void ResponseError(this ILogger logger, string error, string errorDescription, string errorUri);

    [LoggerMessage(52, LogLevel.Error, "Message contains error: '{Error}', error_description: '{ErrorDescription}', error_uri: '{ErrorUri}', status code '{StatusCode}'.", EventName = "ResponseErrorWithStatusCode")]
    public static partial void ResponseErrorWithStatusCode(this ILogger logger, string error, string errorDescription, string errorUri, int statusCode);

    [LoggerMessage(17, LogLevel.Error, "Exception occurred while processing message.", EventName = "ExceptionProcessingMessage")]
    public static partial void ExceptionProcessingMessage(this ILogger logger, Exception ex);

    [LoggerMessage(42, LogLevel.Debug, "The access_token is not available. Claims cannot be retrieved.", EventName = "AccessTokenNotAvailable")]
    public static partial void AccessTokenNotAvailable(this ILogger logger);

    [LoggerMessage(20, LogLevel.Trace, "Retrieving claims from the user info endpoint.", EventName = "RetrievingClaims")]
    public static partial void RetrievingClaims(this ILogger logger);

    [LoggerMessage(22, LogLevel.Debug, "UserInfoEndpoint is not set. Claims cannot be retrieved.", EventName = "UserInfoEndpointNotSet")]
    public static partial void UserInfoEndpointNotSet(this ILogger logger);

    [LoggerMessage(23, LogLevel.Warning, "Failed to un-protect the nonce cookie.", EventName = "UnableToProtectNonceCookie")]
    public static partial void UnableToProtectNonceCookie(this ILogger logger, Exception ex);

    [LoggerMessage(8, LogLevel.Warning, "The redirect URI is not well-formed. The URI is: '{AuthenticationRequestUrl}'.", EventName = "InvalidAuthenticationRequestUrl")]
    public static partial void InvalidAuthenticationRequestUrl(this ILogger logger, string authenticationRequestUrl);

    [LoggerMessage(43, LogLevel.Error, "Unable to read the 'id_token', no suitable ISecurityTokenValidator was found for: '{IdToken}'.", EventName = "UnableToReadIdToken")]
    public static partial void UnableToReadIdToken(this ILogger logger, string idToken);

    [LoggerMessage(40, LogLevel.Error, "The Validated Security Token must be of type JwtSecurityToken, but instead its type is: '{SecurityTokenType}'", EventName = "InvalidSecurityTokenType")]
    public static partial void InvalidSecurityTokenType(this ILogger logger, string? securityTokenType);

    [LoggerMessage(41, LogLevel.Error, "Unable to validate the 'id_token', no suitable ISecurityTokenValidator was found for: '{IdToken}'.", EventName = "UnableToValidateIdToken")]
    public static partial void UnableToValidateIdToken(this ILogger logger, string idToken);

    [LoggerMessage(9, LogLevel.Trace, "Entering {OpenIdConnectHandlerType}'s HandleRemoteAuthenticateAsync.", EventName = "EnteringOpenIdAuthenticationHandlerHandleRemoteAuthenticateAsync")]
    public static partial void EnteringOpenIdAuthenticationHandlerHandleRemoteAuthenticateAsync(this ILogger logger, string openIdConnectHandlerType);

    [LoggerMessage(4, LogLevel.Trace, "Entering {OpenIdConnectHandlerType}'s HandleUnauthorizedAsync.", EventName = "EnteringOpenIdAuthenticationHandlerHandleUnauthorizedAsync")]
    public static partial void EnteringOpenIdAuthenticationHandlerHandleUnauthorizedAsync(this ILogger logger, string openIdConnectHandlerType);

    [LoggerMessage(14, LogLevel.Trace, "Entering {OpenIdConnectHandlerType}'s HandleSignOutAsync.", EventName = "EnteringOpenIdAuthenticationHandlerHandleSignOutAsync")]
    public static partial void EnteringOpenIdAuthenticationHandlerHandleSignOutAsync(this ILogger logger, string openIdConnectHandlerType);

    [LoggerMessage(35, LogLevel.Trace, "User information received: {User}", EventName = "UserInformationReceived")]
    public static partial void UserInformationReceived(this ILogger logger, string user);

    [LoggerMessage(5, LogLevel.Trace, "Using properties.RedirectUri for 'local redirect' post authentication: '{RedirectUri}'.", EventName = "PostAuthenticationLocalRedirect")]
    public static partial void PostAuthenticationLocalRedirect(this ILogger logger, string redirectUri);

    [LoggerMessage(33, LogLevel.Trace, "Using properties.RedirectUri for redirect post authentication: '{RedirectUri}'.", EventName = "PostSignOutRedirect")]
    public static partial void PostSignOutRedirect(this ILogger logger, string redirectUri);

    [LoggerMessage(44, LogLevel.Debug, "RemoteSignOutContext.HandledResponse", EventName = "RemoteSignOutHandledResponse")]
    public static partial void RemoteSignOutHandledResponse(this ILogger logger);

    [LoggerMessage(45, LogLevel.Debug, "RemoteSignOutContext.Skipped", EventName = "RemoteSignOutSkipped")]
    public static partial void RemoteSignOutSkipped(this ILogger logger);

    [LoggerMessage(46, LogLevel.Information, "Remote signout request processed.", EventName = "RemoteSignOut")]
    public static partial void RemoteSignOut(this ILogger logger);

    [LoggerMessage(47, LogLevel.Error, "The remote signout request was ignored because the 'sid' parameter " +
                         "was missing, which may indicate an unsolicited logout.", EventName = "RemoteSignOutSessionIdMissing")]
    public static partial void RemoteSignOutSessionIdMissing(this ILogger logger);

    [LoggerMessage(48, LogLevel.Error, "The remote signout request was ignored because the 'sid' parameter didn't match " +
                         "the expected value, which may indicate an unsolicited logout.", EventName = "RemoteSignOutSessionIdInvalid")]
    public static partial void RemoteSignOutSessionIdInvalid(this ILogger logger);

    [LoggerMessage(49, LogLevel.Information, "AuthenticationScheme: {AuthenticationScheme} signed out.", EventName = "AuthenticationSchemeSignedOut")]
    public static partial void AuthenticationSchemeSignedOut(this ILogger logger, string authenticationScheme);

    [LoggerMessage(53, LogLevel.Debug, "HandleChallenge with Location: {Location}; and Set-Cookie: {Cookie}.", EventName = "HandleChallenge")]
    public static partial void HandleChallenge(this ILogger logger, string location, string cookie);

    [LoggerMessage(54, LogLevel.Error, "The remote signout request was ignored because the 'iss' parameter " +
                        "was missing, which may indicate an unsolicited logout.", EventName = "RemoteSignOutIssuerMissing")]
    public static partial void RemoteSignOutIssuerMissing(this ILogger logger);

    [LoggerMessage(55, LogLevel.Error, "The remote signout request was ignored because the 'iss' parameter didn't match " +
                         "the expected value, which may indicate an unsolicited logout.", EventName = "RemoteSignOutIssuerInvalid")]
    public static partial void RemoteSignOutIssuerInvalid(this ILogger logger);

    [LoggerMessage(56, LogLevel.Error, "Unable to validate the 'id_token', no suitable TokenHandler was found for: '{IdToken}'.", EventName = "UnableToValidateIdTokenFromHandler")]
    public static partial void UnableToValidateIdTokenFromHandler(this ILogger logger, string idToken);

    [LoggerMessage(57, LogLevel.Error, "The Validated Security Token must be of type JsonWebToken, but instead its type is: '{SecurityTokenType}.'", EventName = "InvalidSecurityTokenTypeFromHandler")]
    public static partial void InvalidSecurityTokenTypeFromHandler(this ILogger logger, Type? securityTokenType);
}
