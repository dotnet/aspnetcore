using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Security;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Security
{
    public class SignInManager<TUser> where TUser : class
    {
        private string _authType;
        public string AuthenticationType
        {
            get { return _authType ?? "Microsoft.AspNet.Identity"; }
            set { _authType = value; }
        }

        public UserManager<TUser> UserManager { get; set; }
        public HttpContext Context { get; set; }


        public virtual async Task<ClaimsIdentity> CreateUserIdentityAsync(TUser user)
        {
            if (UserManager == null)
            {
                return null;
            }
            return await UserManager.CreateIdentityAsync(user, AuthenticationType);
        }

        public virtual async Task SignInAsync(TUser user, bool isPersistent, bool rememberBrowser)
        {
            if (Context == null)
            {
                return;
            }

            // TODO: all the two factor logic/external/rememberBrowser
            var userIdentity = await CreateUserIdentityAsync(user);
            Context.Response.SignIn(userIdentity, new AuthenticationProperties { IsPersistent = isPersistent });
        }

        //public virtual async Task<bool> SendTwoFactorCode(string provider)
        //{
        //    var userId = await GetVerifiedUserId();
        //    if (userId == null)
        //    {
        //        return false;
        //    }

        //    var token = await UserManager.GenerateTwoFactorTokenAsync(userId, provider);
        //    // See IdentityConfig.cs to plug in Email/SMS services to actually send the code
        //    await UserManager.NotifyTwoFactorTokenAsync(userId, provider, token);
        //    return true;
        //}

        //public Task<TKey> GetVerifiedUserId()
        //{
        //    //var result = await AuthenticationManager.Authenticate(DefaultAuthenticationTypes.TwoFactorCookie);
        //    //if (result != null && result.Identity != null && !String.IsNullOrEmpty(result.Identity.GetUserIdAsync()))
        //    //{
        //    //    return result.Identity.GetUserIdAsync();
        //    //}
        //    return Task.FromResult(default(TKey));
        //}

        //public async Task<bool> HasBeenVerified()
        //{
        //    return await GetVerifiedUserId() != null;
        //}

        //public virtual async Task<SignInStatus> TwoFactorSignIn(string provider, string code, bool isPersistent, bool rememberBrowser)
        //{
        //    var userId = await GetVerifiedUserId();
        //    if (userId == null)
        //    {
        //        return SignInStatus.Failure;
        //    }
        //    var user = await UserManager.FindByIdAsync(userId);
        //    if (user == null)
        //    {
        //        return SignInStatus.Failure;
        //    }
        //    if (await UserManager.IsLockedOutAsync(user.Id))
        //    {
        //        return SignInStatus.LockedOut;
        //    }
        //    if (await UserManager.VerifyTwoFactorTokenAsync(user.Id, provider, code))
        //    {
        //        // When token is verified correctly, clear the access failed count used for lockout
        //        await UserManager.ResetAccessFailedCountAsync(user.Id);
        //        await SignIn(user, isPersistent, rememberBrowser);
        //        return SignInStatus.Success;
        //    }
        //    // If the token is incorrect, record the failure which also may cause the user to be locked out
        //    await UserManager.AccessFailedAsync(user.Id);
        //    return SignInStatus.Failure;
        //}

        //public async Task<SignInStatus> ExternalSignIn(ExternalLoginInfo loginInfo, bool isPersistent)
        //{
        //    var user = await UserManager.FindByLoginAsync(loginInfo.Login);
        //    if (user == null)
        //    {
        //        return SignInStatus.Failure;
        //    }
        //    if (await UserManager.IsLockedOutAsync(user.Id))
        //    {
        //        return SignInStatus.LockedOut;
        //    }
        //    return await SignInOrTwoFactor(user, isPersistent);
        //}

        //private async Task<SignInStatus> SignInOrTwoFactor(TUser user, bool isPersistent)
        //{
        //    if (await UserManager.GetTwoFactorEnabledAsync(user.Id))
        //        //&& !await AuthenticationManager.TwoFactorBrowserRemembered(user.Id))
        //    {
        //        //var identity = new ClaimsIdentity(DefaultAuthenticationTypes.TwoFactorCookie);
        //        //identity.AddClaimAsync(new Claim(ClaimTypes.NameIdentifier, user.Id));
        //        //AuthenticationManager.SignIn(identity);
        //        return SignInStatus.RequiresTwoFactorAuthentication;
        //    }
        //    await SignIn(user, isPersistent, false);
        //    return SignInStatus.Success;
        //}

        public virtual async Task<SignInStatus> PasswordSignInAsync(string userName, string password, bool isPersistent, bool shouldLockout)
        {
            if (UserManager == null)
            {
                return SignInStatus.Failure;
            }
            var user = await UserManager.FindByNameAsync(userName);
            if (user == null)
            {
                return SignInStatus.Failure;
            }
            if (await UserManager.IsLockedOutAsync(user))
            {
                return SignInStatus.LockedOut;
            }
            if (await UserManager.CheckPasswordAsync(user, password))
            {
                await SignInAsync(user, isPersistent, false);
                return SignInStatus.Success;
                //TODO: return await SignInOrTwoFactor(user, isPersistent);
            }
            if (shouldLockout)
            {
                // If lockout is requested, increment access failed count which might lock out the user
                await UserManager.AccessFailedAsync(user);
                if (await UserManager.IsLockedOutAsync(user))
                {
                    return SignInStatus.LockedOut;
                }
            }
            return SignInStatus.Failure;
        }
    }
}