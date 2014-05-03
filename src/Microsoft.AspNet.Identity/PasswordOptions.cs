using Microsoft.AspNet.ConfigurationModel;

namespace Microsoft.AspNet.Identity
{
    public class PasswordOptions
    {
        public PasswordOptions()
        {
            RequireDigit = true;
            RequireLowercase = true;
            RequireNonLetterOrDigit = true;
            RequireUppercase = true;
            RequiredLength = 6;
        }

        public PasswordOptions(IConfiguration config) : this()
        {
            IdentityOptions.Read(this, config);
        }

        /// <summary>
        ///     Minimum required length
        /// </summary>
        public int RequiredLength { get; set; }

        /// <summary>
        ///     Require a non letter or digit character
        /// </summary>
        public bool RequireNonLetterOrDigit { get; set; }

        /// <summary>
        ///     Require a lower case letter ('a' - 'z')
        /// </summary>
        public bool RequireLowercase { get; set; }

        /// <summary>
        ///     Require an upper case letter ('A' - 'Z')
        /// </summary>
        public bool RequireUppercase { get; set; }

        /// <summary>
        ///     Require a digit ('0' - '9')
        /// </summary>
        public bool RequireDigit { get; set; }

        public virtual void Copy(PasswordOptions options)
        {
            if (options == null)
            {
                return;
            }
            RequireDigit = options.RequireDigit;
            RequireLowercase = options.RequireLowercase;
            RequireNonLetterOrDigit = options.RequireNonLetterOrDigit;
            RequireUppercase = options.RequireUppercase;
            RequiredLength = options.RequiredLength;
        }
    }
}