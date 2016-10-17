// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.Extensions.WebSockets.Internal
{
    public class WebSocketOptions
    {
        /// <summary>
        /// Gets the default ping interval of 30 seconds.
        /// </summary>
        public static TimeSpan DefaultPingInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the default <see cref="WebSocketOptions"/> for an unmasked sender.
        /// </summary>
        /// <remarks>
        /// Uses the default ping interval defined in <see cref="DefaultPingInterval"/>, no masking key,
        /// and automatically responds to pings.
        /// </remarks>
        public static readonly WebSocketOptions DefaultUnmasked = new WebSocketOptions()
        {
            PingInterval = DefaultPingInterval,
            MaskingKeyGenerator = null,
            FixedMaskingKey = null
        };

        /// <summary>
        /// Gets the default <see cref="WebSocketOptions"/> for an unmasked sender.
        /// </summary>
        /// <remarks>
        /// Uses the default ping interval defined in <see cref="DefaultPingInterval"/>, the system random
        /// key generator, and automatically responds to pings.
        /// </remarks>
        public static readonly WebSocketOptions DefaultMasked = new WebSocketOptions()
        {
            PingInterval = DefaultPingInterval,
            MaskingKeyGenerator = RandomNumberGenerator.Create(),
            FixedMaskingKey = null
        };

        /// <summary>
        /// Gets or sets a boolean indicating if all frames, even those automatically handled (<see cref="WebSocketOpcode.Ping"/> and <see cref="WebSocketOpcode.Pong"/> frames),
        /// should be passed to the <see cref="WebSocketConnection.ExecuteAsync"/> callback. NOTE: The frames will STILL be automatically handled, they are
        /// only passed along for diagnostic purposes.
        /// </summary>
        public bool PassAllFramesThrough { get; private set; }

        /// <summary>
        /// Gets or sets the time between pings sent from the local endpoint
        /// </summary>
        public TimeSpan PingInterval { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="RandomNumberGenerator"/> used to generate masking keys used to mask outgoing frames.
        /// If <see cref="FixedMaskingKey"/> is set, this value is ignored. If neither this value nor
        /// <see cref="FixedMaskingKey"/> is set, no masking will be performed.
        /// </summary>
        public RandomNumberGenerator MaskingKeyGenerator { get; internal set; }

        /// <summary>
        /// Gets or sets a fixed masking key used to mask outgoing frames. If this value is set, <see cref="MaskingKeyGenerator"/>
        /// is ignored. If neither this value nor <see cref="MaskingKeyGenerator"/> is set, no masking will be performed.
        /// </summary>
        public byte[] FixedMaskingKey { get; private set; }

        /// <summary>
        /// Sets the ping interval for this <see cref="WebSocketOptions"/>.
        /// </summary>
        /// <param name="pingInterval">The interval at which ping frames will be sent</param>
        /// <returns>A new <see cref="WebSocketOptions"/> with the specified ping interval</returns>
        public WebSocketOptions WithPingInterval(TimeSpan pingInterval)
        {
            return new WebSocketOptions()
            {
                PingInterval = pingInterval,
                FixedMaskingKey = FixedMaskingKey,
                MaskingKeyGenerator = MaskingKeyGenerator
            };
        }

        /// <summary>
        /// Enables frame pass-through in this <see cref="WebSocketOptions"/>. Generally for diagnostic or testing purposes only.
        /// </summary>
        /// <returns>A new <see cref="WebSocketOptions"/> with <see cref="PassAllFramesThrough"/> set to true</returns>
        public WebSocketOptions WithAllFramesPassedThrough()
        {
            return new WebSocketOptions()
            {
                PassAllFramesThrough = true,
                PingInterval = PingInterval,
                FixedMaskingKey = FixedMaskingKey,
                MaskingKeyGenerator = MaskingKeyGenerator
            };
        }

        /// <summary>
        /// Enables random masking in this <see cref="WebSocketOptions"/>, using the system random number generator.
        /// </summary>
        /// <returns>A new <see cref="WebSocketOptions"/> with random masking enabled</returns>
        public WebSocketOptions WithRandomMasking() => WithRandomMasking(RandomNumberGenerator.Create());

        /// <summary>
        /// Enables random masking in this <see cref="WebSocketOptions"/>, using the provided random number generator.
        /// </summary>
        /// <param name="rng">The <see cref="RandomNumberGenerator"/> to use to generate masking keys</param>
        /// <returns>A new <see cref="WebSocketOptions"/> with random masking enabled</returns>
        public WebSocketOptions WithRandomMasking(RandomNumberGenerator rng)
        {
            return new WebSocketOptions()
            {
                PingInterval = PingInterval,
                FixedMaskingKey = null,
                MaskingKeyGenerator = rng
            };
        }

        /// <summary>
        /// Enables fixed masking in this <see cref="WebSocketOptions"/>. FOR DEVELOPMENT PURPOSES ONLY.
        /// </summary>
        /// <param name="maskingKey">The masking key to use for all outgoing frames.</param>
        /// <returns>A new <see cref="WebSocketOptions"/> with fixed masking enabled</returns>
        public WebSocketOptions WithFixedMaskingKey(byte[] maskingKey)
        {
            if (maskingKey.Length != 4)
            {
                throw new ArgumentException("Masking Key must be exactly 4 bytes", nameof(maskingKey));
            }

            return new WebSocketOptions()
            {
                PingInterval = PingInterval,
                FixedMaskingKey = maskingKey,
                MaskingKeyGenerator = null
            };
        }
    }
}
