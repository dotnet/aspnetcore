// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticatorCoreTests
{
    private const string _action = nameof(RemoteAuthenticatorViewCore<RemoteAuthenticationState>.Action);
    private const string _onLogInSucceded = nameof(RemoteAuthenticatorViewCore<RemoteAuthenticationState>.OnLogInSucceeded);
    private const string _onLogOutSucceeded = nameof(RemoteAuthenticatorViewCore<RemoteAuthenticationState>.OnLogOutSucceeded);

    [Fact]
    public async Task AuthenticationManager_Throws_ForInvalidAction()
    {
        // Arrange
        var remoteAuthenticator = new RemoteAuthenticatorViewCore<RemoteAuthenticationState>();

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = ""
        });

        // Act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => remoteAuthenticator.SetParametersAsync(parameters));
    }

    [Fact]
    public async Task AuthenticationManager_Login_NavigatesToReturnUrlOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.SignInCallback = _ => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogIn
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/fetchData", remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Login_DoesNothingOnRedirect()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/login";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl,
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.SignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect,
            State = remoteAuthenticator.AuthenticationState
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogIn
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);

    }

    [Fact]
    public async Task AuthenticationManager_Login_NavigatesToLoginFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.SignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log in."
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogIn
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/authentication/login-failed", remoteAuthenticator.Navigation.Uri.ToString());
        Assert.Equal("There was an error trying to log in.", remoteAuthenticator.Navigation.HistoryEntryState);
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_ThrowsOnRedirectResult()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/");

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogInCallback
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await renderer.Dispatcher.InvokeAsync<object>(async () =>
            {
                await remoteAuthenticator.SetParametersAsync(parameters);
                return null;
            }));
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_DoesNothingOnOperationCompleted()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/login-callback?code=1234";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl);

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.OperationCompleted
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogInCallback
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_NavigatesToReturnUrlFromStateOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState
        });

        var loggingSucceededCalled = false;

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogInCallback,
            [_onLogInSucceded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => loggingSucceededCalled = true),
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(fetchDataUrl, remoteAuthenticator.Navigation.Uri);
        Assert.True(loggingSucceededCalled);
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_NavigatesToLoginFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log in"
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogInCallback
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(
            "https://www.example.com/base/authentication/login-failed",
            remoteAuthenticator.Navigation.Uri);

        Assert.Equal(
            "There was an error trying to log in",
            ((TestNavigationManager)remoteAuthenticator.Navigation).HistoryEntryState);
    }

    [Fact]
    public async Task AuthenticationManager_Callbacks_OnlyExecuteOncePerAction()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login-callback?code=1234");

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
        });

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
        });

        var logInCallbackInvocationCount = 0;
        var logOutCallbackInvocationCount = 0;

        var parameterDictionary = new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogInCallback,
            [_onLogInSucceded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => logInCallbackInvocationCount++),
            [_onLogOutSucceeded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => logOutCallbackInvocationCount++)
        };

        var initialParameters = ParameterView.FromDictionary(parameterDictionary);

        parameterDictionary[_action] = RemoteAuthenticationActions.LogOutCallback;

        var finalParameters = ParameterView.FromDictionary(parameterDictionary);

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(initialParameters));
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(initialParameters));

        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(finalParameters));
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(finalParameters));

        // Assert
        Assert.Equal(1, logInCallbackInvocationCount);
        Assert.Equal(1, logOutCallbackInvocationCount);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_NavigatesToReturnUrlOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout",
            new InteractiveRequestOptions { Interaction = InteractionType.SignOut, ReturnUrl = "https://www.example.com/base/" }.ToState());

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOut
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/", remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_NavigatesToDefaultReturnUrlWhenNoReturnUrlIsPresent()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout");

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOut
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/authentication/logged-out", remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_DoesNothingOnRedirect()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/login";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl,
            new InteractiveRequestOptions { Interaction = InteractionType.SignOut, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect,
            State = remoteAuthenticator.AuthenticationState
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOut
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);

    }

    [Fact]
    public async Task AuthenticationManager_Logout_RedirectsToFailureOnInvalidSignOutState()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        if (remoteAuthenticator.SignOutManager is TestSignOutSessionStateManager testManager)
        {
            testManager.SignOutState = false;
        }

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOut
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(
            "https://www.example.com/base/authentication/logout-failed",
            remoteAuthenticator.Navigation.Uri);

        Assert.Equal(
            "The logout was not initiated from within the page.",
            ((TestNavigationManager)remoteAuthenticator.Navigation).HistoryEntryState);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_NavigatesToLogoutFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log out"
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOut
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/authentication/logout-failed", remoteAuthenticator.Navigation.Uri.ToString());
    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_ThrowsOnRedirectResult()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout-callback",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOutCallback
        });

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await renderer.Dispatcher.InvokeAsync<object>(async () =>
            {
                await remoteAuthenticator.SetParametersAsync(parameters);
                return null;
            }));
    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_DoesNothingOnOperationCompleted()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/logout-callback?code=1234";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl);

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.OperationCompleted
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOutCallback
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_NavigatesToReturnUrlFromStateOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout-callback-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState
        });

        var loggingOutSucceededCalled = false;
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOutCallback,
            [_onLogOutSucceeded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => loggingOutSucceededCalled = true),

        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(fetchDataUrl, remoteAuthenticator.Navigation.Uri);
        Assert.True(loggingOutSucceededCalled);

    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_NavigatesToLoginFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log out"
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = RemoteAuthenticationActions.LogOutCallback
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(
            "https://www.example.com/base/authentication/logout-failed",
            remoteAuthenticator.Navigation.Uri);

        Assert.Equal(
            "There was an error trying to log out",
            ((TestNavigationManager)remoteAuthenticator.Navigation).HistoryEntryState);
    }

    public static TheoryData<UIValidator> DisplaysRightUIData { get; } = new TheoryData<UIValidator>
        {
            { new UIValidator {
                Action = "login", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LoggingIn = validator.FakeRender; } }
            },
            { new UIValidator {
                Action = "login-callback", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.CompletingLoggingIn = validator.FakeRender; } }
            },
            { new UIValidator {
                Action = "login-failed", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogInFailed = m => builder => validator.FakeRender(builder); } }
            },
            { new UIValidator {
                Action = "profile", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LoggingIn = validator.FakeRender; } }
            },
            // Profile fragment overrides
            { new UIValidator {
                Action = "profile", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.UserProfile = validator.FakeRender; } }
            },
            { new UIValidator {
                Action = "register", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LoggingIn = validator.FakeRender; } }
            },
            // Register fragment overrides
            { new UIValidator {
                Action = "register", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.Registering = validator.FakeRender; } }
            },
            { new UIValidator {
                Action = "logout", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogOut = validator.FakeRender; } }
            },
            { new UIValidator {
                Action = "logout-callback", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.CompletingLogOut = validator.FakeRender; } }
            },
            { new UIValidator {
                Action = "logout-failed", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogOutFailed = m => builder => validator.FakeRender(builder); } }
            },
            { new UIValidator {
                Action = "logged-out", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogOutSucceeded = validator.FakeRender; } }
            },
        };

    [Theory]
    [MemberData(nameof(DisplaysRightUIData))]
    public async Task AuthenticationManager_DisplaysRightUI_ForEachStateAsync(UIValidator validator)
    {
        // Arrange
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var testNavigationManager = new TestNavigationManager("https://www.example.com/", "Some error message.", "https://www.example.com/");
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var authenticator = new TestRemoteAuthenticatorView(testNavigationManager);
        authenticator.Logger = logger;
        renderer.Attach(authenticator);
        validator.SetupFakeRender(authenticator);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = validator.Action
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => authenticator.SetParametersAsync(parameters));

        // Assert
        Assert.True(validator.WasCalled);
    }

    [Theory]
    [MemberData(nameof(DisplaysRightUIData))]
    public async Task AuthenticationManager_DoesNotThrowExceptionOnDisplaysUI_WhenPathsAreMissing(UIValidator validator)
    {
        // Arrange
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var testNavigationManager = new TestNavigationManager("https://www.example.com/", "Some error message.", "https://www.example.com/");
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var authenticator = new TestRemoteAuthenticatorView(new RemoteAuthenticationApplicationPathsOptions(), testNavigationManager);
        authenticator.Logger = logger;
        renderer.Attach(authenticator);
        validator.SetupFakeRender(authenticator);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = validator.Action
        });

        // Act
        Task result = await renderer.Dispatcher.InvokeAsync<Task>(() => authenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Null(result.Exception);
    }

    public static TheoryData<UIValidator, string> DisplaysRightUIWhenPathsAreMissingData { get; } = new TheoryData<UIValidator, string>
        {
            // Profile fragment overrides
            {
                new UIValidator {
                    Action = "profile",
                    SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.UserProfile = validator.FakeRender; },
                    RetrieveOriginalRenderAction = (validator, remoteAuthenticator) => { validator.OriginalRender =  remoteAuthenticator.UserProfile; } },
                "ProfileNotSupportedFragment"
            },
            {
                new UIValidator {
                    Action = "register",
                    SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.Registering = validator.FakeRender; },
                    RetrieveOriginalRenderAction = (validator, remoteAuthenticator) => { validator.OriginalRender =  remoteAuthenticator.Registering; } },
                "RegisterNotSupportedFragment"
            }
        };

    [Theory]
    [MemberData(nameof(DisplaysRightUIWhenPathsAreMissingData))]
    public async Task AuthenticationManager_DisplaysRightUI_WhenPathsAreMissing(UIValidator validator, string methodName)
    {
        // Arrange
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var testNavigationManager = new TestNavigationManager("https://www.example.com/", "Some error message.", "https://www.example.com/");
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var authenticator = new TestRemoteAuthenticatorView(new RemoteAuthenticationApplicationPathsOptions(), testNavigationManager);
        authenticator.Logger = logger;
        renderer.Attach(authenticator);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [_action] = validator.Action
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => authenticator.SetParametersAsync(parameters));
        validator.RetrieveOriginalRender(authenticator);
        validator.SetupFakeRender(authenticator);
        Task result = await renderer.Dispatcher.InvokeAsync<Task>(() => authenticator.SetParametersAsync(parameters));

        // Assert
        Assert.True(validator.WasCalled);
        Assert.Equal(methodName, validator.OriginalRender.Method.Name);
    }

    public class UIValidator
    {
        public string Action { get; set; }
        public Action<UIValidator, RemoteAuthenticatorViewCore<RemoteAuthenticationState>> SetupFakeRenderAction { get; set; }
        public Action<UIValidator, RemoteAuthenticatorViewCore<RemoteAuthenticationState>> RetrieveOriginalRenderAction { get; set; }
        public bool WasCalled { get; set; }
        public RenderFragment OriginalRender { get; set; }
        public RenderFragment FakeRender { get; set; }

        public UIValidator() => FakeRender = builder => WasCalled = true;

        internal void SetupFakeRender(TestRemoteAuthenticatorView manager) => SetupFakeRenderAction(this, manager);
        internal void RetrieveOriginalRender(TestRemoteAuthenticatorView manager) => RetrieveOriginalRenderAction(this, manager);
    }

    private static
        (RemoteAuthenticatorViewCore<RemoteAuthenticationState> manager,
        TestRenderer renderer,
        TestRemoteAuthenticationService authenticationServiceMock)

        CreateAuthenticationManager(
        string currentUri,
        string currentState = null,
        string baseUri = "https://www.example.com/base/")
    {
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var remoteAuthenticator = new RemoteAuthenticatorViewCore<RemoteAuthenticationState>();
        remoteAuthenticator.Logger = logger;
        renderer.Attach(remoteAuthenticator);

        var navigationManager = new TestNavigationManager(
            baseUri,
            currentState,
            currentUri);
        remoteAuthenticator.Navigation = navigationManager;

        remoteAuthenticator.AuthenticationState = new RemoteAuthenticationState();
        remoteAuthenticator.ApplicationPaths = new RemoteAuthenticationApplicationPathsOptions();

        var jsRuntime = new TestJsRuntime();
        var authenticationServiceMock = new TestRemoteAuthenticationService(
            jsRuntime,
            Mock.Of<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>(),
            navigationManager);

        remoteAuthenticator.SignOutManager = new TestSignOutSessionStateManager();

        remoteAuthenticator.AuthenticationService = authenticationServiceMock;
        remoteAuthenticator.AuthenticationProvider = authenticationServiceMock;
        return (remoteAuthenticator, renderer, authenticationServiceMock);
    }

    private class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUrl, string currentState, string currentUrl)
        {
            Initialize(baseUrl, currentUrl);
            HistoryEntryState = currentState;
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
            => Uri = System.Uri.IsWellFormedUriString(uri, UriKind.Absolute) ? uri : new Uri(new Uri(BaseUri), uri).ToString();

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = System.Uri.IsWellFormedUriString(uri, UriKind.Absolute) ? uri : new Uri(new Uri(BaseUri), uri).ToString();
            HistoryEntryState = options.HistoryEntryState;
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility
    private class TestSignOutSessionStateManager : SignOutSessionStateManager
#pragma warning restore CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility
    {
        public TestSignOutSessionStateManager() : base(null)
        {
        }
        public bool SignOutState { get; set; } = true;

        public override ValueTask SetSignOutState()
        {
            SignOutState = true;
            return default;
        }

        public override Task<bool> ValidateSignOutState() => Task.FromResult(SignOutState);
    }

    private class TestJsRuntime : IJSRuntime
    {
        public (string identifier, object[] args) LastInvocation { get; set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            LastInvocation = (identifier, args);
            return default;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            LastInvocation = (identifier, args);
            return default;
        }
    }

    public class TestRemoteAuthenticatorView : RemoteAuthenticatorViewCore<RemoteAuthenticationState>
    {
        public TestRemoteAuthenticatorView()
            : this(new RemoteAuthenticationApplicationPathsOptions()
            {
                RemoteProfilePath = "Identity/Account/Manage",
                RemoteRegisterPath = "Identity/Account/Register",
            }, null)
        {
        }

        public TestRemoteAuthenticatorView(NavigationManager manager)
            : this(new RemoteAuthenticationApplicationPathsOptions()
            {
                RemoteProfilePath = "Identity/Account/Manage",
                RemoteRegisterPath = "Identity/Account/Register",
            }, manager)
        {
        }

        public TestRemoteAuthenticatorView(RemoteAuthenticationApplicationPathsOptions applicationPaths, NavigationManager testNavigationManager)
        {
            ApplicationPaths = applicationPaths;
            Navigation = testNavigationManager;
        }

        protected override Task OnParametersSetAsync()
        {
            if (Action == "register" || Action == "profile")
            {
                return base.OnParametersSetAsync();
            }

            return Task.CompletedTask;
        }
    }

    private class TestRemoteAuthenticationService : RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>
    {
        public TestRemoteAuthenticationService(
            IJSRuntime jsRuntime,
            IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> options,
            TestNavigationManager navigationManager) :
            base(jsRuntime, options, navigationManager, new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()), null)
        {
        }

        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> SignInCallback { get; set; }
        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> CompleteSignInCallback { get; set; }
        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> SignOutCallback { get; set; }
        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> CompleteSignOutCallback { get; set; }
        public Func<ValueTask<ClaimsPrincipal>> GetAuthenticatedUserCallback { get; set; }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await GetAuthenticatedUserCallback());

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => CompleteSignInCallback(context);

        protected internal override ValueTask<ClaimsPrincipal> GetAuthenticatedUser() => GetAuthenticatedUserCallback();

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => CompleteSignOutCallback(context);

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => SignInCallback(context);

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => SignOutCallback(context);
    }

    private class TestRenderer : Renderer
    {
        public TestRenderer(IServiceProvider services)
            : base(services, NullLoggerFactory.Instance)
        {
        }

        public int Attach(IComponent component) => AssignRootComponentId(component);

        private static readonly Dispatcher _dispatcher = Dispatcher.CreateDefault();

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) =>
            Task.CompletedTask;
    }
}
