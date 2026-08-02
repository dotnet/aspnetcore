// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public partial class RemoteAuthenticatorViewCore<TAuthenticationState> where TAuthenticationState : RemoteAuthenticationState
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Processing action {Action}.", EventName = nameof(ProcessingAuthenticatorAction))]
        public static partial void ProcessingAuthenticatorAction(ILogger logger, string? action);

        [LoggerMessage(2, LogLevel.Debug, "Login completed successfully.", EventName = nameof(LoginCompletedSuccessfully))]
        public static partial void LoginCompletedSuccessfully(ILogger logger);

        [LoggerMessage(3, LogLevel.Debug, "Login requires redirect to the identity provider.", EventName = nameof(LoginRequiresRedirect))]
        public static partial void LoginRequiresRedirect(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Navigating to {Url}.", EventName = nameof(NavigatingToUrl))]
        public static partial void NavigatingToUrl(ILogger logger, [StringSyntax(StringSyntaxAttribute.Uri)] string url);

        [LoggerMessage(5, LogLevel.Debug, "Raising LoginCompleted event.", EventName = nameof(InvokingLoginCompletedCallback))]
        public static partial void InvokingLoginCompletedCallback(ILogger logger);

        [LoggerMessage(6, LogLevel.Debug, "Login operation failed with error '{ErrorMessage}'.", EventName = nameof(LoginFailed))]
        public static partial void LoginFailed(ILogger logger, string errorMessage);

        [LoggerMessage(7, LogLevel.Debug, "Login callback failed with error '{ErrorMessage}'.", EventName = nameof(LoginCallbackFailed))]
        public static partial void LoginCallbackFailed(ILogger logger, string errorMessage);

        [LoggerMessage(8, LogLevel.Debug, "Login redirect completed successfully.", EventName = nameof(LoginRedirectCompletedSuccessfully))]
        public static partial void LoginRedirectCompletedSuccessfully(ILogger logger);

        [LoggerMessage(9, LogLevel.Debug, "The logout was not initiated from within the page.", EventName = nameof(LogoutOperationInitiatedExternally))]
        public static partial void LogoutOperationInitiatedExternally(ILogger logger);

        [LoggerMessage(10, LogLevel.Debug, "Logout completed successfully.", EventName = nameof(LogoutCompletedSuccessfully))]
        public static partial void LogoutCompletedSuccessfully(ILogger logger);

        [LoggerMessage(11, LogLevel.Debug, "Logout requires redirect to the identity provider.", EventName = nameof(LogoutRequiresRedirect))]
        public static partial void LogoutRequiresRedirect(ILogger logger);

        [LoggerMessage(12, LogLevel.Debug, "Raising LogoutCompleted event.", EventName = nameof(InvokingLogoutCompletedCallback))]
        public static partial void InvokingLogoutCompletedCallback(ILogger logger);

        [LoggerMessage(13, LogLevel.Debug, "Logout operation failed with error '{ErrorMessage}'.", EventName = nameof(LogoutFailed))]
        public static partial void LogoutFailed(ILogger logger, string errorMessage);

        [LoggerMessage(14, LogLevel.Debug, "Logout callback failed with error '{ErrorMessage}'.", EventName = nameof(LogoutCallbackFailed))]
        public static partial void LogoutCallbackFailed(ILogger logger, string errorMessage);

        [LoggerMessage(15, LogLevel.Debug, "Logout redirect completed successfully.", EventName = nameof(LogoutRedirectCompletedSuccessfully))]
        public static partial void LogoutRedirectCompletedSuccessfully(ILogger logger);
    }
}
