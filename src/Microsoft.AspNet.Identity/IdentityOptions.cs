using System.Reflection;
using Microsoft.AspNet.ConfigurationModel;

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

        public IdentityOptions(IConfiguration config)
        {
            ClaimType = new ClaimTypeOptions(config.GetSubKey("identity:claimtype"));
            User = new UserOptions(config.GetSubKey("identity:user"));
            Password = new PasswordOptions(config.GetSubKey("identity:password"));
            //Lockout = new LockoutOptions(config.GetSubKey("identity:lockout"));
        }

        public static void Read(object obj, IConfiguration config)
        {
            var type = obj.GetType();
            var props = type.GetTypeInfo().DeclaredProperties;
            foreach (var prop in props)
            {
                // TODO: handle non string types?
                if (!prop.CanWrite)
                {
                    continue;
                }
                var configValue = config.Get(prop.Name);
                if (configValue == null)
                {
                    continue;
                }
                if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(obj, configValue);
                }
                else if (prop.PropertyType == typeof(int))
                {
                    // todo: TryParse/ errors?
                    prop.SetValue(obj, int.Parse(configValue));
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    // todo: TryParse/ errors?
                    prop.SetValue(obj, bool.Parse(configValue));
                }
                //else if (prop.PropertyType == typeof(TimeSpan))
                //{
                //    // todo: TryParse/ errors?
                //    prop.SetValue(obj, TimeSpan.Parse(configValue));
                //}
            }
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