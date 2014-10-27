using Microsoft.AspNet.Security.DataProtection;
using Microsoft.Framework.OptionsModel;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    public class DataProtectionTokenProviderOptions
    {
        public string Name { get; set; } = "DataProtection";
        public TimeSpan TokenLifespan { get; set; } = TimeSpan.FromDays(1);
    }

    /// <summary>
    ///     Token provider that uses an IDataProtector to generate encrypted tokens based off of the security stamp
    /// </summary>
    public class DataProtectorTokenProvider<TUser> : IUserTokenProvider<TUser> where TUser : class
    {
        public DataProtectorTokenProvider(IDataProtectionProvider dataProtectionProvider, IOptions<DataProtectionTokenProviderOptions> options)
        {
            if (options == null || options.Options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (dataProtectionProvider == null)
            {
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            }
            Options = options.Options;
            // Use the Name as the purpose which should usually be distinct from others
            Protector = dataProtectionProvider.CreateProtector(Name ?? "DataProtectorTokenProvider"); 
        }

        public DataProtectionTokenProviderOptions Options { get; private set; }
        public IDataProtector Protector { get; private set; }

        public string Name { get { return Options.Name; } }

        /// <summary>
        ///     Generate a protected string for a user
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var ms = new MemoryStream();
            var userId = await manager.GetUserIdAsync(user, cancellationToken);
            using (var writer = ms.CreateWriter())
            {
                writer.Write(DateTimeOffset.UtcNow);
                writer.Write(userId);
                writer.Write(purpose ?? "");
                string stamp = null;
                if (manager.SupportsUserSecurityStamp)
                {
                    stamp = await manager.GetSecurityStampAsync(user);
                }
                writer.Write(stamp ?? "");
            }
            var protectedBytes = Protector.Protect(ms.ToArray());
            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        ///     Return false if the token is not valid
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var unprotectedData = Protector.Unprotect(Convert.FromBase64String(token));
                var ms = new MemoryStream(unprotectedData);
                using (var reader = ms.CreateReader())
                {
                    var creationTime = reader.ReadDateTimeOffset();
                    var expirationTime = creationTime + Options.TokenLifespan;
                    if (expirationTime < DateTimeOffset.UtcNow)
                    {
                        return false;
                    }

                    var userId = reader.ReadString();
                    var actualUserId = await manager.GetUserIdAsync(user, cancellationToken);
                    if (userId != actualUserId)
                    {
                        return false;
                    }
                    var purp = reader.ReadString();
                    if (!string.Equals(purp, purpose))
                    {
                        return false;
                    }
                    var stamp = reader.ReadString();
                    if (reader.PeekChar() != -1)
                    {
                        return false;
                    }

                    if (manager.SupportsUserSecurityStamp)
                    {
                        return stamp == await manager.GetSecurityStampAsync(user);
                    }
                    return stamp == "";
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // Do not leak exception
            }
            return false;
        }

        /// <summary>
        ///     Returns false because tokens are two long to be used for two factor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        /// <summary>
        ///     This provider no-ops by default when asked to notify a user
        /// </summary>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task NotifyAsync(string token, UserManager<TUser> manager, TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
    }

    // Based on Levi's authentication sample
    internal static class StreamExtensions
    {
        internal static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        public static BinaryReader CreateReader(this Stream stream)
        {
            return new BinaryReader(stream, DefaultEncoding, true);
        }

        public static BinaryWriter CreateWriter(this Stream stream)
        {
            return new BinaryWriter(stream, DefaultEncoding, true);
        }

        public static DateTimeOffset ReadDateTimeOffset(this BinaryReader reader)
        {
            return new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
        }

        public static void Write(this BinaryWriter writer, DateTimeOffset value)
        {
            writer.Write(value.UtcTicks);
        }
    }
}