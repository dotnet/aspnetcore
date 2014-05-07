using System.Reflection;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Configuration for lockout
    /// </summary>
    public class IdentityOptions
    {

        public IdentityOptions()
        {
            // TODO: Split into sub options
            ClaimType = new ClaimTypeOptions();
            User = new UserOptions();
            Password = new PasswordOptions();
            Lockout = new LockoutOptions();
        }

        public ClaimTypeOptions ClaimType { get; set; }

        public UserOptions User { get; set; }

        public PasswordOptions Password { get; set; }

        public LockoutOptions Lockout { get; set; }

        // TODO: maybe make this internal as its only for tests
        public void Copy(IdentityOptions options)
        {
            if (options == null)
            {
                return;
            }
            User.Copy(options.User);
            Password.Copy(options.Password);
            Lockout.Copy(options.Lockout);
            ClaimType.Copy(options.ClaimType);
        }
    }
}