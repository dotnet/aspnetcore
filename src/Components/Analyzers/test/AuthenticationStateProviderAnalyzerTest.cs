// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class AuthenticationStateProviderAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new AuthenticationStateProviderAnalyzer();

    private static readonly string TestDeclarations = @"
    namespace System.Threading.Tasks
    {
        public class Task<TResult>
        {
            public TResult Result => default;
        }
    }

    namespace Microsoft.AspNetCore.Components.Authorization
    {
        public class AuthenticationState
        {
            public AuthenticationState(System.Security.Claims.ClaimsPrincipal user) { }
            public System.Security.Claims.ClaimsPrincipal User { get; }
        }

        public delegate void AuthenticationStateChangedHandler(System.Threading.Tasks.Task<AuthenticationState> task);

        public abstract class AuthenticationStateProvider
        {
            public abstract System.Threading.Tasks.Task<AuthenticationState> GetAuthenticationStateAsync();
            public event AuthenticationStateChangedHandler AuthenticationStateChanged;
            protected void NotifyAuthenticationStateChanged(System.Threading.Tasks.Task<AuthenticationState> task) { }
        }
    }

    namespace System
    {
        public interface IDisposable
        {
            void Dispose();
        }
    }
";

    [Fact]
    public void NoDiagnostic_WhenSubscribesToAuthenticationStateChanged()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class MyComponent : System.IDisposable
        {
            private AuthenticationStateProvider _provider;

            public MyComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
                _provider.AuthenticationStateChanged += OnAuthStateChanged;
            }

            private async void OnAuthStateChanged(Task<AuthenticationState> task)
            {
            }

            private async Task LoadUserAsync()
            {
                var state = await _provider.GetAuthenticationStateAsync();
            }

            public void Dispose()
            {
                _provider.AuthenticationStateChanged -= OnAuthStateChanged;
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnostic_WhenDoesNotCallGetAuthenticationStateAsync()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;

    namespace TestApp
    {
        class MyComponent
        {
            private AuthenticationStateProvider _provider;

            public MyComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void Diagnostic_WhenCallsGetAuthenticationStateAsyncWithoutSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class MyComponent
        {
            private AuthenticationStateProvider _provider;

            public MyComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }

            private async Task LoadUserAsync()
            {
                var state = await _provider.GetAuthenticationStateAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0012",
                Message = "'MyComponent' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 15) }
            });
    }

    [Fact]
    public void Diagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncWithoutSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class MyCustomProvider : AuthenticationStateProvider
        {
            private Task<AuthenticationState> _cachedState;

            public override Task<AuthenticationState> GetAuthenticationStateAsync()
            {
                return _cachedState;
            }

            public void RefreshState()
            {
                _cachedState = GetAuthenticationStateAsync();
            }
        }

        class ConsumerComponent
        {
            private AuthenticationStateProvider _provider;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }

            private async Task LoadUserAsync()
            {
                var state = await _provider.GetAuthenticationStateAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0012",
                Message = "'MyCustomProvider' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",

                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 15) }
            },
            new DiagnosticResult
            {
                Id = "BL0012",
                Message = "'ConsumerComponent' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 22, 15) }
            });
    }

    [Fact]
    public void NoDiagnostic_WhenTypeDoesNotUseAuthenticationStateProvider()
    {
        var test = @"
    namespace TestApp
    {
        class UnrelatedComponent
        {
            private string _name;

            public UnrelatedComponent(string name)
            {
                _name = name;
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnostic_WhenAuthenticationStateProviderIsProperty()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class MyComponent : System.IDisposable
        {
            public AuthenticationStateProvider Provider { get; set; }

            public void Initialize()
            {
                Provider.AuthenticationStateChanged += OnAuthStateChanged;
            }

            private async void OnAuthStateChanged(Task<AuthenticationState> task)
            {
            }

            private async Task LoadUserAsync()
            {
                var state = await Provider.GetAuthenticationStateAsync();
            }

            public void Dispose()
            {
                Provider.AuthenticationStateChanged -= OnAuthStateChanged;
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }
}
