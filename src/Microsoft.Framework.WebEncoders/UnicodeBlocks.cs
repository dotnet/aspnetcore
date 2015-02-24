// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Contains predefined Unicode code point filters.
    /// </summary>
    public static partial class UnicodeBlocks
    {
        /// <summary>
        /// Represents an empty Unicode block.
        /// </summary>
        /// <remarks>
        /// This block contains no code points.
        /// </remarks>
        public static UnicodeBlock None
        {
            get
            {
                return Volatile.Read(ref _none) ?? CreateEmptyBlock(ref _none);
            }
        }
        private static UnicodeBlock _none;

        /// <summary>
        /// Represents a block containing all characters in the Unicode Basic Multilingual Plane (U+0000..U+FFFF).
        /// </summary>
        public static UnicodeBlock All
        {
            get
            {
                return Volatile.Read(ref _all) ?? CreateBlock(ref _all, '\u0000', '\uFFFF');
            }
        }
        private static UnicodeBlock _all;

        [MethodImpl(MethodImplOptions.NoInlining)] // the caller should be inlined, not this method
        private static UnicodeBlock CreateBlock(ref UnicodeBlock block, char first, char last)
        {
            // If the block hasn't been created, create it now.
            // It's ok if two threads race and one overwrites the other's 'block' value.
            Debug.Assert(last > first, "Code points were specified out of order.");
            var newBlock = UnicodeBlock.FromCharacterRange(first, last);
            Volatile.Write(ref block, newBlock);
            return newBlock;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the caller should be inlined, not this method
        private static UnicodeBlock CreateEmptyBlock(ref UnicodeBlock block)
        {
            // If the block hasn't been created, create it now.
            // It's ok if two threads race and one overwrites the other's 'block' value.
            var newBlock = new UnicodeBlock(0, 0);
            Volatile.Write(ref block, newBlock);
            return newBlock;
        }
    }
}
