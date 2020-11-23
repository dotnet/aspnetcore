// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
                "https://www.example.com/base/authentication/login?returnUrl=https://www.example.com/base/fetchData");

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
            Assert.Equal("https://www.example.com/base/fetchData", jsRuntime.LastInvocation.args[0]);
        }

        [Fact]
        public async Task AuthenticationManager_Login_DoesNothingOnRedirect()
        {
            // Arrange
            var originalUrl = "https://www.example.com/base/authentication/login?returnUrl=https://www.example.com/base/fetchData";
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(originalUrl);

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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
                "https://www.example.com/base/authentication/login?returnUrl=https://www.example.com/base/fetchData");

            authServiceMock.SignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
            {
                Status = RemoteAuthenticationStatus.Failure,
                ErrorMessage = "There was an error trying to log in"
            });

            var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [_action] = RemoteAuthenticationActions.LogIn
            });

            // Act
            await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

            // Assert
            Assert.Equal("https://www.example.com/base/authentication/login-failed", remoteAuthenticator.Navigation.Uri.ToString());
        }

        [Fact]
        public async Task AuthenticationManager_LoginCallback_ThrowsOnRedirectResult()
        {
            // Arrange
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
                "https://www.example.com/base/authentication/login?returnUrl=https://www.example.com/base/fetchData");

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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
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
            Assert.Equal(fetchDataUrl, jsRuntime.LastInvocation.args[0]);
            Assert.True(loggingSucceededCalled);

        }

        [Fact]
        public async Task AuthenticationManager_LoginCallback_NavigatesToLoginFailureOnError()
        {
            // Arrange
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
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
                "https://www.example.com/base/authentication/login-failed?message=There was an error trying to log in",
                jsRuntime.LastInvocation.args[0]);

        }

        [Fact]
        public async Task AuthenticationManager_Logout_NavigatesToReturnUrlOnSuccess()
        {
            // Arrange
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
                "https://www.example.com/base/authentication/logout?returnUrl=https://www.example.com/base/");

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
            Assert.Equal("https://www.example.com/base/", jsRuntime.LastInvocation.args[0]);
        }

        [Fact]
        public async Task AuthenticationManager_Logout_NavigatesToDefaultReturnUrlWhenNoReturnUrlIsPresent()
        {
            // Arrange
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
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
            Assert.Equal("https://www.example.com/base/authentication/logged-out", jsRuntime.LastInvocation.args[0]);
        }

        [Fact]
        public async Task AuthenticationManager_Logout_DoesNothingOnRedirect()
        {
            // Arrange
            var originalUrl = "https://www.example.com/base/authentication/login?returnUrl=https://www.example.com/base/fetchData";
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(originalUrl);

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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
                "https://www.example.com/base/authentication/logout?returnUrl=https://www.example.com/base/fetchData");

            if(remoteAuthenticator.SignOutManager is TestSignOutSessionStateManager testManager)
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
                "https://www.example.com/base/authentication/logout-failed?message=The%20logout%20was%20not%20initiated%20from%20within%20the%20page.",
                remoteAuthenticator.Navigation.Uri);
        }

        [Fact]
        public async Task AuthenticationManager_Logout_NavigatesToLogoutFailureOnError()
        {
            // Arrange
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
                "https://www.example.com/base/authentication/logout?returnUrl=https://www.example.com/base/fetchData");

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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
                "https://www.example.com/base/authentication/logout-callback?returnUrl=https://www.example.com/base/fetchData");

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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
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
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
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
            Assert.Equal(fetchDataUrl, jsRuntime.LastInvocation.args[0]);
            Assert.True(loggingOutSucceededCalled);

        }

        [Fact]
        public async Task AuthenticationManager_LogoutCallback_NavigatesToLoginFailureOnError()
        {
            // Arrange
            var (remoteAuthenticator, renderer, authServiceMock, jsRuntime) = CreateAuthenticationManager(
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
                "https://www.example.com/base/authentication/logout-failed?message=There was an error trying to log out",
                jsRuntime.LastInvocation.args[0]);

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
            var authenticator = new TestRemoteAuthenticatorView();
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
            var authenticator = new TestRemoteAuthenticatorView(new RemoteAuthenticationApplicationPathsOptions());
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
            var jsRuntime = new TestJsRuntime();
            var authenticator = new TestRemoteAuthenticatorView(new RemoteAuthenticationApplicationPathsOptions(), jsRuntime);
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
            Assert.Equal(default, jsRuntime.LastInvocation);
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
            TestRemoteAuthenticationService authenticationServiceMock,
            TestJsRuntime js)

            CreateAuthenticationManager(
            string currentUri,
            string baseUri = "https://www.example.com/base/")
        {
            var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
            var remoteAuthenticator = new RemoteAuthenticatorViewCore<RemoteAuthenticationState>();
            renderer.Attach(remoteAuthenticator);

            var navigationManager = new TestNavigationManager(
                baseUri,
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
            remoteAuthenticator.JS = jsRuntime;
            return (remoteAuthenticator, renderer, authenticationServiceMock, jsRuntime);
        }

        private class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager(string baseUrl, string currentUrl) => Initialize(baseUrl, currentUrl);

            protected override void NavigateToCore(string uri, bool forceLoad)
                => Uri = System.Uri.IsWellFormedUriString(uri, UriKind.Absolute) ? uri : new Uri(new Uri(BaseUri), uri).ToString();
        }

        private class TestSignOutSessionStateManager : SignOutSessionStateManager
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
            {
                ApplicationPaths = new RemoteAuthenticationApplicationPathsOptions()
                {
                    RemoteProfilePath = "Identity/Account/Manage",
                    RemoteRegisterPath = "Identity/Account/Register",
                };
            }

            public TestRemoteAuthenticatorView(RemoteAuthenticationApplicationPathsOptions applicationPaths, IJSRuntime jsRuntime = default)
            {
                ApplicationPaths = applicationPaths;
                JS = jsRuntime;
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
                base(jsRuntime, options, navigationManager, new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()))
            {
            }

            public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> SignInCallback { get; set; }
            public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> CompleteSignInCallback { get; set; }
            public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> SignOutCallback { get; set; }
            public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> CompleteSignOutCallback { get; set; }
            public Func<ValueTask<ClaimsPrincipal>> GetAuthenticatedUserCallback { get; set; }

            public async override Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await GetAuthenticatedUserCallback());

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
}
