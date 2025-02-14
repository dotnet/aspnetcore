// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides protection and validation of identity tokens.
/// </summary>
/// <typeparam name="TUser">The type used to represent a user.</typeparam>
public class DataProtectorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectorTokenProvider{TUser}"/> class.
    /// </summary>
    /// <param name="dataProtectionProvider">The system data protection provider.</param>
    /// <param name="options">The configured <see cref="DataProtectionTokenProviderOptions"/>.</param>
    /// <param name="logger">The logger used to log messages, warnings and errors.</param>
    public DataProtectorTokenProvider(IDataProtectionProvider dataProtectionProvider,
                                      IOptions<DataProtectionTokenProviderOptions> options,
                                      ILogger<DataProtectorTokenProvider<TUser>> logger)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);

        Options = options?.Value ?? new DataProtectionTokenProviderOptions();

        // Use the Name as the purpose which should usually be distinct from others
        Protector = dataProtectionProvider.CreateProtector(Name ?? "DataProtectorTokenProvider");
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the <see cref="DataProtectionTokenProviderOptions"/> for this instance.
    /// </summary>
    /// <value>
    /// The <see cref="DataProtectionTokenProviderOptions"/> for this instance.
    /// </value>
    protected DataProtectionTokenProviderOptions Options { get; private set; }

    /// <summary>
    /// Gets the <see cref="IDataProtector"/> for this instance.
    /// </summary>
    /// <value>
    /// The <see cref="IDataProtector"/> for this instance.
    /// </value>
    protected IDataProtector Protector { get; private set; }

    /// <summary>
    /// Gets the name of this instance.
    /// </summary>
    /// <value>
    /// The name of this instance.
    /// </value>
    public string Name { get { return Options.Name; } }

    /// <summary>
    /// Gets the <see cref="ILogger"/> used to log messages from the provider.
    /// </summary>
    /// <value>
    /// The <see cref="ILogger"/> used to log messages from the provider.
    /// </value>
    public ILogger<DataProtectorTokenProvider<TUser>> Logger { get; }

    /// <summary>
    /// Generates a protected token for the specified <paramref name="user"/> as an asynchronous operation.
    /// </summary>
    /// <param name="purpose">The purpose the token will be used for.</param>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve user properties from.</param>
    /// <param name="user">The <typeparamref name="TUser"/> the token will be generated from.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the generated token.</returns>
    public virtual async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var ms = new MemoryStream();
        var userId = await manager.GetUserIdAsync(user);
        using (var writer = ms.CreateWriter())
        {
            writer.Write(DateTimeOffset.UtcNow);
            writer.Write(userId);
            writer.Write(purpose ?? "");
            string? stamp = null;
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
    /// Validates the protected <paramref name="token"/> for the specified <paramref name="user"/> and <paramref name="purpose"/> as an asynchronous operation.
    /// </summary>
    /// <param name="purpose">The purpose the token was be used for.</param>
    /// <param name="token">The token to validate.</param>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve user properties from.</param>
    /// <param name="user">The <typeparamref name="TUser"/> the token was generated for.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous validation,
    /// containing true if the token is valid, otherwise false.
    /// </returns>
    public virtual async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
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
                    Logger.InvalidExpirationTime();
                    return false;
                }

                var userId = reader.ReadString();
                var actualUserId = await manager.GetUserIdAsync(user);
                if (userId != actualUserId)
                {
                    Logger.UserIdsNotEquals();
                    return false;
                }

                var purp = reader.ReadString();
                if (!string.Equals(purp, purpose))
                {
                    Logger.PurposeNotEquals(purpose, purp);
                    return false;
                }

                var stamp = reader.ReadString();
                if (reader.PeekChar() != -1)
                {
                    Logger.UnexpectedEndOfInput();
                    return false;
                }

                if (manager.SupportsUserSecurityStamp)
                {
                    var isEqualsSecurityStamp = stamp == await manager.GetSecurityStampAsync(user);
                    if (!isEqualsSecurityStamp)
                    {
                        Logger.SecurityStampNotEquals();
                    }

                    return isEqualsSecurityStamp;
                }

                var stampIsEmpty = stamp == "";
                if (!stampIsEmpty)
                {
                    Logger.SecurityStampIsNotEmpty();
                }

                return stampIsEmpty;
            }
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch
        {
            // Do not leak exception
            Logger.UnhandledException();
        }

        return false;
    }

    /// <summary>
    /// Returns a <see cref="bool"/> indicating whether a token generated by this instance
    /// can be used as a Two Factor Authentication token as an asynchronous operation.
    /// </summary>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve user properties from.</param>
    /// <param name="user">The <typeparamref name="TUser"/> the token was generated for.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query,
    /// containing true if a token generated by this instance can be used as a Two Factor Authentication token, otherwise false.
    /// </returns>
    /// <remarks>This method will always return false for instances of <see cref="DataProtectorTokenProvider{TUser}"/>.</remarks>
    public virtual Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
    {
        return Task.FromResult(false);
    }
}

/// <summary>
/// Utility extensions to streams
/// </summary>
internal static class StreamExtensions
{
    internal static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

    public static BinaryReader CreateReader(this Stream stream)
        => new BinaryReader(stream, DefaultEncoding, true);

    public static BinaryWriter CreateWriter(this Stream stream)
        => new BinaryWriter(stream, DefaultEncoding, true);

    public static DateTimeOffset ReadDateTimeOffset(this BinaryReader reader)
        => new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);

    public static void Write(this BinaryWriter writer, DateTimeOffset value)
    {
        writer.Write(value.UtcTicks);
    }
}
