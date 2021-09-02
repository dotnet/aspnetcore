// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods for <see cref="ISession"/>.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Sets an int value in the <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to assign.</param>
        /// <param name="value">The value to assign.</param>
        public static void SetInt32(this ISession session, string key, int value)
        {
            var bytes = new byte[]
            {
                (byte)(value >> 24),
                (byte)(0xFF & (value >> 16)),
                (byte)(0xFF & (value >> 8)),
                (byte)(0xFF & value)
            };
            session.Set(key, bytes);
        }

        /// <summary>
        /// Gets an int value from <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to read.</param>
        public static int? GetInt32(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null || data.Length < 4)
            {
                return null;
            }
            return data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
        }
        
		/// <summary>
		/// Sets an int value in the <see cref="ISession"/>.
		/// </summary>
		/// <param name="session">The <see cref="ISession"/>.</param>
		/// <param name="key">The key to assign.</param>
		/// <param name="value">The value to assign.</param>
		public static void SetInt64(this ISession session, string key, long value)
		{
			var bytes = new byte[]
			{
				(byte)(value >> 56),
				(byte)(0xFF & value >> 48),
				(byte)(0xFF & value >> 40),
				(byte)(0xFF & value >> 32),
				(byte)(0xFF & value >> 24),
				(byte)(0xFF & (value >> 16)),
				(byte)(0xFF & (value >> 8)),
				(byte)(0xFF & value)
			};
			session.Set(key, bytes);
		}

		/// <summary>
		/// Gets a long value from <see cref="ISession"/>.
		/// </summary>
		/// <param name="session">The <see cref="ISession"/>.</param>
		/// <param name="key">The key to read.</param>
		public static long? GetInt64(this ISession session, string key)
		{
			var data = session.Get(key);
			if (data == null || data.Length < 8)
			{
				return null;
			}
			return data[0] << 56 | data[1] << 48 | data[2] << 40 | data[3] << 32 | data[4] << 24 | data[5] << 16 | data[6] << 8 | data[7];
		}

		/// <summary>
		/// Sets a DateTime value in the <see cref="ISession"/>.
		/// </summary>
		/// <param name="session">The <see cref="ISession"/>.</param>
		/// <param name="key">The key to assign.</param>
		/// <param name="value">The value to assign.</param>
		public static void SetDateTime(this ISession session, string key, DateTime value)
		{
			session.SetInt64(key, value.Ticks);
		}

		/// <summary>
		/// Gets a DateTime value from <see cref="ISession"/>.
		/// </summary>
		/// <param name="session">The <see cref="ISession"/>.</param>
		/// <param name="key">The key to read.</param>
		public static DateTime? GetDateTime(this ISession session, string key)
		{
			var data = session.GetInt64(key);
			if (data == null)
			{
				return null;
			}
			return new DateTime(data.Value);
		}

        /// <summary>
        /// Sets a <see cref="string"/> value in the <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to assign.</param>
        /// <param name="value">The value to assign.</param>
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Gets a string value from <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to read.</param>
        public static string? GetString(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Gets a byte-array value from <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to read.</param>
        public static byte[]? Get(this ISession session, string key)
        {
            session.TryGetValue(key, out var value);
            return value;
        }
    }
}
