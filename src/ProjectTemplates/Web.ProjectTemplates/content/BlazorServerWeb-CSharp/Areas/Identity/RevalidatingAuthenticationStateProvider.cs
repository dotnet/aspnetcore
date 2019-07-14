using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorServerWeb_CSharp.Areas.Identity
{
    /// <summary>
    /// An <see cref="AuthenticationStateProvider"/> service that revalidates the
    /// authentication state at regular intervals. If a signed-in user's security
    /// stamp changes, this revalidation mechanism will sign the user out.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class RevalidatingAuthenticationStateProvider<TUser>
        : AuthenticationStateProvider, IDisposable where TUser : class
    {
        private readonly static TimeSpan RevalidationInterval = TimeSpan.FromMinutes(30);

        private readonly CancellationTokenSource _loopCancellationTokenSource = new CancellationTokenSource();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private Task<AuthenticationState> _currentAuthenticationStateTask;

        public RevalidatingAuthenticationStateProvider(
            IServiceScopeFactory scopeFactory,
            SignInManager<TUser> circuitScopeSignInManager,
            ILogger<RevalidatingAuthenticationStateProvider<TUser>> logger)
        {
            var initialUser = circuitScopeSignInManager.Context.User;
            _currentAuthenticationStateTask = Task.FromResult(new AuthenticationState(initialUser));
            _scopeFactory = scopeFactory;
            _logger = logger;

            if (initialUser.Identity.IsAuthenticated)
            {
                _ = RevalidationLoop();
            }
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => _currentAuthenticationStateTask;

        private async Task RevalidationLoop()
        {
            var cancellationToken = _loopCancellationTokenSource.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(RevalidationInterval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                var isValid = await CheckIfAuthenticationStateIsValidAsync();
                if (!isValid)
                {
                    // Force sign-out. Also stop the revalidation loop, because the user can
                    // only sign back in by starting a new connection.
                    var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
                    _currentAuthenticationStateTask = Task.FromResult(new AuthenticationState(anonymousUser));
                    NotifyAuthenticationStateChanged(_currentAuthenticationStateTask);
                    _loopCancellationTokenSource.Cancel();
                }
            }
        }

        private async Task<bool> CheckIfAuthenticationStateIsValidAsync()
        {
            try
            {
                // Get the sign-in manager from a new scope to ensure it fetches fresh data
                using (var scope = _scopeFactory.CreateScope())
                {
                    var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<TUser>>();
                    var authenticationState = await _currentAuthenticationStateTask;
                    var validatedUser = await signInManager.ValidateSecurityStampAsync(authenticationState.User);
                    return validatedUser != null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while revalidating authentication state");
                return false;
            }
        }

        void IDisposable.Dispose()
            => _loopCancellationTokenSource.Cancel();
    }
}
