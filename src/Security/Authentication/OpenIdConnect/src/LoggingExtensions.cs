// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _redirectToIdentityProviderForSignOutHandledResponse;
        private static Action<ILogger, Exception> _redirectToIdentityProviderHandledResponse;
        private static Action<ILogger, Exception> _signOutCallbackRedirectHandledResponse;
        private static Action<ILogger, Exception> _signOutCallbackRedirectSkipped;
        private static Action<ILogger, Exception> _updatingConfiguration;
        private static Action<ILogger, Exception> _receivedIdToken;
        private static Action<ILogger, Exception> _redeemingCodeForTokens;
        private static Action<ILogger, string, Exception> _enteringOpenIdAuthenticationHandlerHandleRemoteAuthenticateAsync;
        private static Action<ILogger, string, Exception> _enteringOpenIdAuthenticationHandlerHandleUnauthorizedAsync;
        private static Action<ILogger, string, Exception> _enteringOpenIdAuthenticationHandlerHandleSignOutAsync;
        private static Action<ILogger, string, Exception> _messageReceived;
        private static Action<ILogger, Exception> _messageReceivedContextHandledResponse;
        private static Action<ILogger, Exception> _messageReceivedContextSkipped;
        private static Action<ILogger, Exception> _authorizationCodeReceived;
        private static Action<ILogger, Exception> _configurationManagerRequestRefreshCalled;
        private static Action<ILogger, Exception> _tokenResponseReceived;
        private static Action<ILogger, Exception> _tokenValidatedHandledResponse;
        private static Action<ILogger, Exception> _tokenValidatedSkipped;
        private static Action<ILogger, Exception> _authenticationFailedContextHandledResponse;
        private static Action<ILogger, Exception> _authenticationFailedContextSkipped;
        private static Action<ILogger, Exception> _authorizationCodeReceivedContextHandledResponse;
        private static Action<ILogger, Exception> _authorizationCodeReceivedContextSkipped;
        private static Action<ILogger, Exception> _tokenResponseReceivedHandledResponse;
        private static Action<ILogger, Exception> _tokenResponseReceivedSkipped;
        private static Action<ILogger, string, Exception> _userInformationReceived;
        private static Action<ILogger, Exception> _userInformationReceivedHandledResponse;
        private static Action<ILogger, Exception> _userInformationReceivedSkipped;
        private static Action<ILogger, string, Exception> _invalidLogoutQueryStringRedirectUrl;
        private static Action<ILogger, Exception> _nullOrEmptyAuthorizationResponseState;
        private static Action<ILogger, Exception> _unableToReadAuthorizationResponseState;
        private static Action<ILogger, string, string, string, Exception> _responseError;
        private static Action<ILogger, string, string, string, int, Exception> _responseErrorWithStatusCode;
        private static Action<ILogger, Exception> _exceptionProcessingMessage;
        private static Action<ILogger, Exception> _accessTokenNotAvailable;
        private static Action<ILogger, Exception> _retrievingClaims;
        private static Action<ILogger, Exception> _userInfoEndpointNotSet;
        private static Action<ILogger, Exception> _unableToProtectNonceCookie;
        private static Action<ILogger, string, Exception> _invalidAuthenticationRequestUrl;
        private static Action<ILogger, string, Exception> _unableToReadIdToken;
        private static Action<ILogger, string, Exception> _invalidSecurityTokenType;
        private static Action<ILogger, string, Exception> _unableToValidateIdToken;
        private static Action<ILogger, string, Exception> _postAuthenticationLocalRedirect;
        private static Action<ILogger, string, Exception> _postSignOutRedirect;
        private static Action<ILogger, Exception> _remoteSignOutHandledResponse;
        private static Action<ILogger, Exception> _remoteSignOutSkipped;
        private static Action<ILogger, Exception> _remoteSignOut;
        private static Action<ILogger, Exception> _remoteSignOutSessionIdMissing;
        private static Action<ILogger, Exception> _remoteSignOutSessionIdInvalid;
        private static Action<ILogger, string, Exception> _authenticationSchemeSignedOut;
        private static Action<ILogger, string, string, Exception> _handleChallenge;
        private static Action<ILogger, Exception> _remoteSignOutIssuerMissing;
        private static Action<ILogger, Exception> _remoteSignOutIssuerInvalid;

        static LoggingExtensions()
        {
            // Final
            _redirectToIdentityProviderForSignOutHandledResponse = LoggerMessage.Define(
                eventId: new EventId(1, "RedirectToIdentityProviderForSignOutHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "RedirectToIdentityProviderForSignOut.HandledResponse");
            _invalidLogoutQueryStringRedirectUrl = LoggerMessage.Define<string>(
                eventId: new EventId(3, "InvalidLogoutQueryStringRedirectUrl"),
                logLevel: LogLevel.Warning,
                formatString: "The query string for Logout is not a well-formed URI. Redirect URI: '{RedirectUrl}'.");
            _enteringOpenIdAuthenticationHandlerHandleUnauthorizedAsync = LoggerMessage.Define<string>(
                eventId: new EventId(4, "EnteringOpenIdAuthenticationHandlerHandleUnauthorizedAsync"),
                logLevel: LogLevel.Trace,
                formatString: "Entering {OpenIdConnectHandlerType}'s HandleUnauthorizedAsync.");
            _enteringOpenIdAuthenticationHandlerHandleSignOutAsync = LoggerMessage.Define<string>(
                eventId: new EventId(14, "EnteringOpenIdAuthenticationHandlerHandleSignOutAsync"),
                logLevel: LogLevel.Trace,
                formatString: "Entering {OpenIdConnectHandlerType}'s HandleSignOutAsync.");
            _postAuthenticationLocalRedirect = LoggerMessage.Define<string>(
                eventId: new EventId(5, "PostAuthenticationLocalRedirect"),
                logLevel: LogLevel.Trace,
                formatString: "Using properties.RedirectUri for 'local redirect' post authentication: '{RedirectUri}'.");
            _redirectToIdentityProviderHandledResponse = LoggerMessage.Define(
                eventId: new EventId(6, "RedirectToIdentityProviderHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "RedirectToIdentityProvider.HandledResponse");
            _invalidAuthenticationRequestUrl = LoggerMessage.Define<string>(
                eventId: new EventId(8, "InvalidAuthenticationRequestUrl"),
                logLevel: LogLevel.Warning,
                formatString: "The redirect URI is not well-formed. The URI is: '{AuthenticationRequestUrl}'.");
            _enteringOpenIdAuthenticationHandlerHandleRemoteAuthenticateAsync = LoggerMessage.Define<string>(
                eventId: new EventId(9, "EnteringOpenIdAuthenticationHandlerHandleRemoteAuthenticateAsync"),
                logLevel: LogLevel.Trace,
                formatString: "Entering {OpenIdConnectHandlerType}'s HandleRemoteAuthenticateAsync.");
            _nullOrEmptyAuthorizationResponseState = LoggerMessage.Define(
                eventId: new EventId(10, "NullOrEmptyAuthorizationResponseState"),
                logLevel: LogLevel.Debug,
                formatString: "message.State is null or empty.");
            _unableToReadAuthorizationResponseState = LoggerMessage.Define(
                eventId: new EventId(11, "UnableToReadAuthorizationResponseState"),
                logLevel: LogLevel.Debug,
                formatString: "Unable to read the message.State.");
            _responseError = LoggerMessage.Define<string, string, string>(
                eventId: new EventId(12, "ResponseError"),
                logLevel: LogLevel.Error,
                formatString: "Message contains error: '{Error}', error_description: '{ErrorDescription}', error_uri: '{ErrorUri}'.");
            _responseErrorWithStatusCode = LoggerMessage.Define<string, string, string, int>(
                eventId: new EventId(52, "ResponseErrorWithStatusCode"),
                logLevel: LogLevel.Error,
                formatString: "Message contains error: '{Error}', error_description: '{ErrorDescription}', error_uri: '{ErrorUri}', status code '{StatusCode}'.");
            _updatingConfiguration = LoggerMessage.Define(
                eventId: new EventId(13, "UpdatingConfiguration"),
                logLevel: LogLevel.Debug,
                formatString: "Updating configuration");
            _tokenValidatedHandledResponse = LoggerMessage.Define(
                eventId: new EventId(15, "TokenValidatedHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "TokenValidated.HandledResponse");
            _tokenValidatedSkipped = LoggerMessage.Define(
                eventId: new EventId(16, "TokenValidatedSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "TokenValidated.Skipped");
            _exceptionProcessingMessage = LoggerMessage.Define(
                eventId: new EventId(17, "ExceptionProcessingMessage"),
                logLevel: LogLevel.Error,
                formatString: "Exception occurred while processing message.");
            _configurationManagerRequestRefreshCalled = LoggerMessage.Define(
                eventId: new EventId(18, "ConfigurationManagerRequestRefreshCalled"),
                logLevel: LogLevel.Debug,
                formatString: "Exception of type 'SecurityTokenSignatureKeyNotFoundException' thrown, Options.ConfigurationManager.RequestRefresh() called.");
            _redeemingCodeForTokens = LoggerMessage.Define(
                eventId: new EventId(19, "RedeemingCodeForTokens"),
                logLevel: LogLevel.Debug,
                formatString: "Redeeming code for tokens.");
            _retrievingClaims = LoggerMessage.Define(
                eventId: new EventId(20, "RetrievingClaims"),
                logLevel: LogLevel.Trace,
                formatString: "Retrieving claims from the user info endpoint.");
            _receivedIdToken = LoggerMessage.Define(
                eventId: new EventId(21, "ReceivedIdToken"),
                logLevel: LogLevel.Debug,
                formatString: "Received 'id_token'");
            _userInfoEndpointNotSet = LoggerMessage.Define(
                eventId: new EventId(22, "UserInfoEndpointNotSet"),
                logLevel: LogLevel.Debug,
                formatString: "UserInfoEndpoint is not set. Claims cannot be retrieved.");
            _unableToProtectNonceCookie = LoggerMessage.Define(
                eventId: new EventId(23, "UnableToProtectNonceCookie"),
                logLevel: LogLevel.Warning,
                formatString: "Failed to un-protect the nonce cookie.");
            _messageReceived = LoggerMessage.Define<string>(
                eventId: new EventId(24, "MessageReceived"),
                logLevel: LogLevel.Trace,
                formatString: "MessageReceived: '{RedirectUrl}'.");
            _messageReceivedContextHandledResponse = LoggerMessage.Define(
                eventId: new EventId(25, "MessageReceivedContextHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "MessageReceivedContext.HandledResponse");
            _messageReceivedContextSkipped = LoggerMessage.Define(
                eventId: new EventId(26, "MessageReceivedContextSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "MessageReceivedContext.Skipped");
            _authorizationCodeReceived = LoggerMessage.Define(
                eventId: new EventId(27, "AuthorizationCodeReceived"),
                logLevel: LogLevel.Trace,
                formatString: "Authorization code received.");
            _authorizationCodeReceivedContextHandledResponse = LoggerMessage.Define(
                eventId: new EventId(28, "AuthorizationCodeReceivedContextHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "AuthorizationCodeReceivedContext.HandledResponse");
            _authorizationCodeReceivedContextSkipped = LoggerMessage.Define(
                eventId: new EventId(29, "AuthorizationCodeReceivedContextSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "AuthorizationCodeReceivedContext.Skipped");
            _tokenResponseReceived = LoggerMessage.Define(
                eventId: new EventId(30, "TokenResponseReceived"),
                logLevel: LogLevel.Trace,
                formatString: "Token response received.");
            _tokenResponseReceivedHandledResponse = LoggerMessage.Define(
                eventId: new EventId(31, "TokenResponseReceivedHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "TokenResponseReceived.HandledResponse");
            _tokenResponseReceivedSkipped = LoggerMessage.Define(
                eventId: new EventId(32, "TokenResponseReceivedSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "TokenResponseReceived.Skipped");
            _postSignOutRedirect = LoggerMessage.Define<string>(
                eventId: new EventId(33, "PostSignOutRedirect"),
                logLevel: LogLevel.Trace,
                formatString: "Using properties.RedirectUri for redirect post authentication: '{RedirectUri}'.");
            _userInformationReceived = LoggerMessage.Define<string>(
                eventId: new EventId(35, "UserInformationReceived"),
                 logLevel: LogLevel.Trace,
                formatString: "User information received: {User}");
            _userInformationReceivedHandledResponse = LoggerMessage.Define(
                eventId: new EventId(36, "UserInformationReceivedHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "The UserInformationReceived event returned Handled.");
            _userInformationReceivedSkipped = LoggerMessage.Define(
                eventId: new EventId(37, "UserInformationReceivedSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "The UserInformationReceived event returned Skipped.");
            _authenticationFailedContextHandledResponse = LoggerMessage.Define(
                eventId: new EventId(38, "AuthenticationFailedContextHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "AuthenticationFailedContext.HandledResponse");
            _authenticationFailedContextSkipped = LoggerMessage.Define(
                eventId: new EventId(39, "AuthenticationFailedContextSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "AuthenticationFailedContext.Skipped");
            _invalidSecurityTokenType = LoggerMessage.Define<string>(
                eventId: new EventId(40, "InvalidSecurityTokenType"),
                logLevel: LogLevel.Error,
                formatString: "The Validated Security Token must be of type JwtSecurityToken, but instead its type is: '{SecurityTokenType}'");
            _unableToValidateIdToken = LoggerMessage.Define<string>(
                eventId: new EventId(41, "UnableToValidateIdToken"),
                logLevel: LogLevel.Error,
                formatString: "Unable to validate the 'id_token', no suitable ISecurityTokenValidator was found for: '{IdToken}'.");
            _accessTokenNotAvailable = LoggerMessage.Define(
                eventId: new EventId(42, "AccessTokenNotAvailable"),
                logLevel: LogLevel.Debug,
                formatString: "The access_token is not available. Claims cannot be retrieved.");
            _unableToReadIdToken = LoggerMessage.Define<string>(
                eventId: new EventId(43, "UnableToReadIdToken"),
                logLevel: LogLevel.Error,
                formatString: "Unable to read the 'id_token', no suitable ISecurityTokenValidator was found for: '{IdToken}'.");
            _remoteSignOutHandledResponse = LoggerMessage.Define(
                eventId: new EventId(44, "RemoteSignOutHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "RemoteSignOutContext.HandledResponse");
            _remoteSignOutSkipped = LoggerMessage.Define(
                eventId: new EventId(45, "RemoteSignOutSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "RemoteSignOutContext.Skipped");
            _remoteSignOut = LoggerMessage.Define(
                eventId: new EventId(46, "RemoteSignOut"),
                logLevel: LogLevel.Information,
                formatString: "Remote signout request processed.");
            _remoteSignOutSessionIdMissing = LoggerMessage.Define(
                eventId: new EventId(47, "RemoteSignOutSessionIdMissing"),
                logLevel: LogLevel.Error,
                formatString: "The remote signout request was ignored because the 'sid' parameter " +
                             "was missing, which may indicate an unsolicited logout.");
            _remoteSignOutSessionIdInvalid = LoggerMessage.Define(
                eventId: new EventId(48, "RemoteSignOutSessionIdInvalid"),
                logLevel: LogLevel.Error,
                formatString: "The remote signout request was ignored because the 'sid' parameter didn't match " +
                             "the expected value, which may indicate an unsolicited logout.");
            _authenticationSchemeSignedOut = LoggerMessage.Define<string>(
                eventId: new EventId(49, "AuthenticationSchemeSignedOut"),
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} signed out.");
            _signOutCallbackRedirectHandledResponse = LoggerMessage.Define(
                eventId: new EventId(50, "SignOutCallbackRedirectHandledResponse"),
                logLevel: LogLevel.Debug,
                formatString: "RedirectToSignedOutRedirectUri.HandledResponse");
            _signOutCallbackRedirectSkipped = LoggerMessage.Define(
                eventId: new EventId(51, "SignOutCallbackRedirectSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "RedirectToSignedOutRedirectUri.Skipped");

            //  EventId 52 is used by ResponseErrorWithStatusCode
            _handleChallenge = LoggerMessage.Define<string, string>(
                eventId: new EventId(53, "HandleChallenge"),
                logLevel: LogLevel.Debug,
                formatString: "HandleChallenge with Location: {Location}; and Set-Cookie: {Cookie}.");
            _remoteSignOutIssuerMissing = LoggerMessage.Define(
              eventId: new EventId(54, "RemoteSignOutIssuerMissing"),
              logLevel: LogLevel.Error,
              formatString: "The remote signout request was ignored because the 'iss' parameter " +
                            "was missing, which may indicate an unsolicited logout.");
            _remoteSignOutIssuerInvalid = LoggerMessage.Define(
              eventId: new EventId(55, "RemoteSignOutIssuerInvalid"),
               logLevel: LogLevel.Error,
               formatString: "The remote signout request was ignored because the 'iss' parameter didn't match " +
                             "the expected value, which may indicate an unsolicited logout.");
        }

        public static void UpdatingConfiguration(this ILogger logger)
        {
            _updatingConfiguration(logger, null);
        }

        public static void ConfigurationManagerRequestRefreshCalled(this ILogger logger)
        {
            _configurationManagerRequestRefreshCalled(logger, null);
        }

        public static void AuthorizationCodeReceived(this ILogger logger)
        {
            _authorizationCodeReceived(logger, null);
        }

        public static void TokenResponseReceived(this ILogger logger)
        {
            _tokenResponseReceived(logger, null);
        }

        public static void ReceivedIdToken(this ILogger logger)
        {
            _receivedIdToken(logger, null);
        }

        public static void RedeemingCodeForTokens(this ILogger logger)
        {
            _redeemingCodeForTokens(logger, null);
        }

        public static void TokenValidatedHandledResponse(this ILogger logger)
        {
            _tokenValidatedHandledResponse(logger, null);
        }

        public static void TokenValidatedSkipped(this ILogger logger)
        {
            _tokenValidatedSkipped(logger, null);
        }

        public static void AuthorizationCodeReceivedContextHandledResponse(this ILogger logger)
        {
            _authorizationCodeReceivedContextHandledResponse(logger, null);
        }

        public static void AuthorizationCodeReceivedContextSkipped(this ILogger logger)
        {
            _authorizationCodeReceivedContextSkipped(logger, null);
        }

        public static void TokenResponseReceivedHandledResponse(this ILogger logger)
        {
            _tokenResponseReceivedHandledResponse(logger, null);
        }

        public static void TokenResponseReceivedSkipped(this ILogger logger)
        {
            _tokenResponseReceivedSkipped(logger, null);
        }

        public static void AuthenticationFailedContextHandledResponse(this ILogger logger)
        {
            _authenticationFailedContextHandledResponse(logger, null);
        }

        public static void AuthenticationFailedContextSkipped(this ILogger logger)
        {
            _authenticationFailedContextSkipped(logger, null);
        }

        public static void MessageReceived(this ILogger logger, string redirectUrl)
        {
            _messageReceived(logger, redirectUrl, null);
        }

        public static void MessageReceivedContextHandledResponse(this ILogger logger)
        {
            _messageReceivedContextHandledResponse(logger, null);
        }

        public static void MessageReceivedContextSkipped(this ILogger logger)
        {
            _messageReceivedContextSkipped(logger, null);
        }

        public static void RedirectToIdentityProviderForSignOutHandledResponse(this ILogger logger)
        {
            _redirectToIdentityProviderForSignOutHandledResponse(logger, null);
        }

        public static void RedirectToIdentityProviderHandledResponse(this ILogger logger)
        {
            _redirectToIdentityProviderHandledResponse(logger, null);
        }

        public static void SignOutCallbackRedirectHandledResponse(this ILogger logger)
        {
            _signOutCallbackRedirectHandledResponse(logger, null);
        }

        public static void SignOutCallbackRedirectSkipped(this ILogger logger)
        {
            _signOutCallbackRedirectSkipped(logger, null);
        }

        public static void UserInformationReceivedHandledResponse(this ILogger logger)
        {
            _userInformationReceivedHandledResponse(logger, null);
        }

        public static void UserInformationReceivedSkipped(this ILogger logger)
        {
            _userInformationReceivedSkipped(logger, null);
        }

        public static void InvalidLogoutQueryStringRedirectUrl(this ILogger logger, string redirectUrl)
        {
            _invalidLogoutQueryStringRedirectUrl(logger, redirectUrl, null);
        }

        public static void NullOrEmptyAuthorizationResponseState(this ILogger logger)
        {
            _nullOrEmptyAuthorizationResponseState(logger, null);
        }

        public static void UnableToReadAuthorizationResponseState(this ILogger logger)
        {
            _unableToReadAuthorizationResponseState(logger, null);
        }

        public static void ResponseError(this ILogger logger, string error, string errorDescription, string errorUri)
        {
            _responseError(logger, error, errorDescription, errorUri, null);
        }

        public static void ResponseErrorWithStatusCode(this ILogger logger, string error, string errorDescription, string errorUri, int statusCode)
        {
            _responseErrorWithStatusCode(logger, error, errorDescription, errorUri, statusCode, null);
        }

        public static void ExceptionProcessingMessage(this ILogger logger, Exception ex)
        {
            _exceptionProcessingMessage(logger, ex);
        }

        public static void AccessTokenNotAvailable(this ILogger logger)
        {
            _accessTokenNotAvailable(logger, null);
        }

        public static void RetrievingClaims(this ILogger logger)
        {
            _retrievingClaims(logger, null);
        }

        public static void UserInfoEndpointNotSet(this ILogger logger)
        {
            _userInfoEndpointNotSet(logger, null);
        }

        public static void UnableToProtectNonceCookie(this ILogger logger, Exception ex)
        {
            _unableToProtectNonceCookie(logger, ex);
        }

        public static void InvalidAuthenticationRequestUrl(this ILogger logger, string redirectUri)
        {
            _invalidAuthenticationRequestUrl(logger, redirectUri, null);
        }

        public static void UnableToReadIdToken(this ILogger logger, string idToken)
        {
            _unableToReadIdToken(logger, idToken, null);
        }

        public static void InvalidSecurityTokenType(this ILogger logger, string tokenType)
        {
            _invalidSecurityTokenType(logger, tokenType, null);
        }

        public static void UnableToValidateIdToken(this ILogger logger, string idToken)
        {
            _unableToValidateIdToken(logger, idToken, null);
        }

        public static void EnteringOpenIdAuthenticationHandlerHandleRemoteAuthenticateAsync(this ILogger logger, string openIdConnectHandlerTypeName)
        {
            _enteringOpenIdAuthenticationHandlerHandleRemoteAuthenticateAsync(logger, openIdConnectHandlerTypeName, null);
        }

        public static void EnteringOpenIdAuthenticationHandlerHandleUnauthorizedAsync(this ILogger logger, string openIdConnectHandlerTypeName)
        {
            _enteringOpenIdAuthenticationHandlerHandleUnauthorizedAsync(logger, openIdConnectHandlerTypeName, null);
        }

        public static void EnteringOpenIdAuthenticationHandlerHandleSignOutAsync(this ILogger logger, string openIdConnectHandlerTypeName)
        {
            _enteringOpenIdAuthenticationHandlerHandleSignOutAsync(logger, openIdConnectHandlerTypeName, null);
        }

        public static void UserInformationReceived(this ILogger logger, string user)
        {
            _userInformationReceived(logger, user, null);
        }

        public static void PostAuthenticationLocalRedirect(this ILogger logger, string redirectUri)
        {
            _postAuthenticationLocalRedirect(logger, redirectUri, null);
        }

        public static void PostSignOutRedirect(this ILogger logger, string redirectUri)
        {
            _postSignOutRedirect(logger, redirectUri, null);
        }

        public static void RemoteSignOutHandledResponse(this ILogger logger)
        {
            _remoteSignOutHandledResponse(logger, null);
        }

        public static void RemoteSignOutSkipped(this ILogger logger)
        {
            _remoteSignOutSkipped(logger, null);
        }

        public static void RemoteSignOut(this ILogger logger)
        {
            _remoteSignOut(logger, null);
        }

        public static void RemoteSignOutSessionIdMissing(this ILogger logger)
        {
            _remoteSignOutSessionIdMissing(logger, null);
        }

        public static void RemoteSignOutSessionIdInvalid(this ILogger logger)
        {
            _remoteSignOutSessionIdInvalid(logger, null);
        }

        public static void AuthenticationSchemeSignedOut(this ILogger logger, string authenticationScheme)
        {
            _authenticationSchemeSignedOut(logger, authenticationScheme, null);
        }

        public static void HandleChallenge(this ILogger logger, string location, string cookie)
            => _handleChallenge(logger, location, cookie, null);

        public static void RemoteSignOutIssuerMissing(this ILogger logger)
        {
            _remoteSignOutIssuerMissing(logger, null);
        }

        public static void RemoteSignOutIssuerInvalid(this ILogger logger)
        {
            _remoteSignOutIssuerInvalid(logger, null);
        }
    }
}
