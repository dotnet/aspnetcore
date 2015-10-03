// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.WebEncoders
{
    internal static class EncoderCommon
    {
        // Gets the optimal capacity of the StringBuilder that will be used to build the output
        // given a specified number of input characters and the worst-case growth.
        public static int GetCapacityOfOutputStringBuilder(int numCharsToEncode, int worstCaseOutputCharsPerInputChar)
        {
            // We treat 32KB byte size (16k chars) as a soft upper boundary for the length of any StringBuilder
            // that we allocate. We'll try to avoid going above this boundary if we can avoid it so that we
            // don't allocate objects on the LOH.
            const int upperBound = 16 * 1024;

            // Once we have chosen an initial value for the StringBuilder size, the StringBuilder type will
            // efficiently allocate additionally blocks if necessary.

            if (numCharsToEncode >= upperBound)
            {
                // We know that the output will contain at least as many characters as the input, so if the
                // input length exceeds the soft upper boundary just preallocate the entire builder and hope for
                // a best-case outcome.
                return numCharsToEncode;
            }
            else
            {
                // Allocate the worst-case if we can, but don't exceed the soft upper boundary.
                long worstCaseTotalChars = (long)numCharsToEncode * worstCaseOutputCharsPerInputChar;
                return (int)Math.Min(upperBound, worstCaseTotalChars);
            }
        }
    }
}
