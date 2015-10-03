// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Contains predefined <see cref="UnicodeRange"/> instances which correspond to blocks
    /// from the Unicode 8.0 specification.
    /// </summary>
    public static partial class UnicodeRanges
    {
        /// <summary>
        /// An empty <see cref="UnicodeRange"/>. This range contains no code points.
        /// </summary>
        public static UnicodeRange None => Volatile.Read(ref _none) ?? CreateEmptyRange(ref _none);
        private static UnicodeRange _none;

        /// <summary>
        /// A <see cref="UnicodeRange"/> which contains all characters in the Unicode Basic
        /// Multilingual Plane (U+0000..U+FFFF).
        /// </summary>
        public static UnicodeRange All => Volatile.Read(ref _all) ?? CreateRange(ref _all, '\u0000', '\uFFFF');
        private static UnicodeRange _all;

        [MethodImpl(MethodImplOptions.NoInlining)] // the caller should be inlined, not this method
        private static UnicodeRange CreateEmptyRange(ref UnicodeRange range)
        {
            // If the range hasn't been created, create it now.
            // It's ok if two threads race and one overwrites the other's 'range' value.
            var newRange = new UnicodeRange(0, 0);
            Volatile.Write(ref range, newRange);
            return newRange;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the caller should be inlined, not this method
        private static UnicodeRange CreateRange(ref UnicodeRange range, char first, char last)
        {
            // If the range hasn't been created, create it now.
            // It's ok if two threads race and one overwrites the other's 'range' value.
            Debug.Assert(last > first, "Code points were specified out of order.");
            var newRange = UnicodeRange.FromSpan(first, last);
            Volatile.Write(ref range, newRange);
            return newRange;
        }
    }
}
