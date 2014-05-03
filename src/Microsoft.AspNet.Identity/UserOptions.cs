using Microsoft.AspNet.ConfigurationModel;

namespace Microsoft.AspNet.Identity
{
    public class UserOptions
    {
        public UserOptions()
        {
            AllowOnlyAlphanumericNames = true;
            //User.RequireUniqueEmail = true; // TODO: app decision?
        }

        public UserOptions(IConfiguration config) : this()
        {
            IdentityOptions.Read(this, config);
        }

        /// <summary>
        ///     Only allow [A-Za-z0-9@_] in UserNames
        /// </summary>
        public bool AllowOnlyAlphanumericNames { get; set; }

        /// <summary>
        ///     If set, enforces that emails are non empty, valid, and unique
        /// </summary>
        public bool RequireUniqueEmail { get; set; }

        public virtual void Copy(UserOptions options)
        {
            if (options == null)
            {
                return;
            }
            AllowOnlyAlphanumericNames = options.AllowOnlyAlphanumericNames;
            RequireUniqueEmail = options.RequireUniqueEmail;
        }
    }
}