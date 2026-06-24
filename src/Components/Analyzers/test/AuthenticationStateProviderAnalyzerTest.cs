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
                Id = "BL0013",
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
                Id = "BL0013",
                Message = "'MyCustomProvider' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",

                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 15) }
            },
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "'ConsumerComponent' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 22, 15) }
            });
    }

    [Fact]
    public void Diagnostic_WhenOnlyUnsubscribedToAuthenticationStateChanged()
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

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "'MyComponent' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",

                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 15) }
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

    [Fact]
    public void Diagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderWithoutSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            protected AuthenticationStateProvider _provider;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }
        }

        class Consumer2Component : ConsumerComponent
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await _provider.GetAuthenticationStateAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "'Consumer2Component' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 15) }
            });
    }

    [Fact]
    public void NoDiagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderWithSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            protected AuthenticationStateProvider _provider;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }
        }

        class Consumer2Component : ConsumerComponent
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await _provider.GetAuthenticationStateAsync();
                _provider.AuthenticationStateChanged += OnAuthStateChanged;
            }

            private async void OnAuthStateChanged(Task<AuthenticationState> task)
            {
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void Diagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderThroughPropWithoutSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            private AuthenticationStateProvider _provider;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }

            protected AuthenticationStateProvider Provider => _provider;
        }

        class Consumer2Component : ConsumerComponent
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await Provider.GetAuthenticationStateAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "'Consumer2Component' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 15) }
            });
    }

    [Fact]
    public void NoDiagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderThroughPropWithSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            private AuthenticationStateProvider _provider;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }

            protected AuthenticationStateProvider Provider => _provider;
        }

        class Consumer2Component : ConsumerComponent
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await Provider.GetAuthenticationStateAsync();
                Provider.AuthenticationStateChanged += OnAuthStateChanged;
            }

            private async void OnAuthStateChanged(Task<AuthenticationState> task)
            {
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void Diagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderTwoFieldsWithoutSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            private AuthenticationStateProvider _provider;
            protected AuthenticationStateProvider _provider2;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
                _provider2 = provider;
            }

            private AuthenticationStateProvider Provider => _provider;
        }

        class Consumer2Component : ConsumerComponent
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await _provider2.GetAuthenticationStateAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "'Consumer2Component' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 15) }
            });
    }

    [Fact]
    public void NoDiagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderTwoFieldsWithSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            private AuthenticationStateProvider _provider;
            protected AuthenticationStateProvider _provider2;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
                _provider2 = provider;
            }

            private AuthenticationStateProvider Provider => _provider;
        }

        class Consumer2Component : ConsumerComponent
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await _provider2.GetAuthenticationStateAsync();
                _provider2.AuthenticationStateChanged += OnAuthStateChanged;
            }

            private async void OnAuthStateChanged(Task<AuthenticationState> task)
            {
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void Diagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderThroughPropFurtherDownWithoutSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            private AuthenticationStateProvider _provider;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }

            protected AuthenticationStateProvider Provider => _provider;
        }

        class ConsumerComponent1 : ConsumerComponent
        {
            private AuthenticationStateProvider _hiddenProvider;

            public ConsumerComponent1(AuthenticationStateProvider provider) : base(provider) {
                _hiddenProvider = provider;
            }

            private AuthenticationStateProvider HiddenProvider => _hiddenProvider;
        }

        class Consumer2Component : ConsumerComponent1
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await Provider.GetAuthenticationStateAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "'Consumer2Component' calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 30, 15) }
            });
    }

    [Fact]
    public void NoDiagnostic_WhenDerivedTypeCallsGetAuthenticationStateAsyncOnParentProviderThroughPropFurtherDownWithSubscription()
    {
        var test = @"
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Threading.Tasks;

    namespace TestApp
    {
        class ConsumerComponent
        {
            private AuthenticationStateProvider _provider;

            public ConsumerComponent(AuthenticationStateProvider provider)
            {
                _provider = provider;
            }

            protected AuthenticationStateProvider Provider => _provider;
        }

        class ConsumerComponent1 : ConsumerComponent
        {
            private AuthenticationStateProvider _hiddenProvider;

            public ConsumerComponent1(AuthenticationStateProvider provider) : base(provider) {
                _hiddenProvider = provider;
            }

            private AuthenticationStateProvider HiddenProvider => _hiddenProvider;
        }

        class Consumer2Component : ConsumerComponent1
        {
            public Consumer2Component(AuthenticationStateProvider provider) : base(provider) { }

            private async Task LoadUserAsync()
            {
                var state = await Provider.GetAuthenticationStateAsync();
                Provider.AuthenticationStateChanged += OnAuthStateChanged;
            }

            private async void OnAuthStateChanged(Task<AuthenticationState> task)
            {
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }
}
