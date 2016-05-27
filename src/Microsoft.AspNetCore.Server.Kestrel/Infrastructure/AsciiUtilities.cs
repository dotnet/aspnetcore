// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    internal class AsciiUtilities
    {
        public static unsafe bool TryGetAsciiString(byte* input, char* output, int count)
        {
            var i = 0;
            int orValue = 0;
            bool hasZero = false;
            while (i < count - 11)
            {
                orValue |= *input | *(input + 1) | *(input + 2) | *(input + 3) | *(input + 4) | *(input + 5) |
                    *(input + 6) | *(input + 7) | *(input + 8) | *(input + 9) | *(input + 10) | *(input + 11);
                hasZero = hasZero || *input == 0 || *(input + 1) == 0 || *(input + 2) == 0 || *(input + 3) == 0 ||
                    *(input + 4) == 0 || *(input + 5) == 0 || *(input + 6) == 0 || *(input + 7) == 0 ||
                    *(input + 8) == 0 || *(input + 9) == 0 || *(input + 10) == 0 || *(input + 11) == 0;

                i += 12;
                *(output) = (char)*(input);
                *(output + 1) = (char)*(input + 1);
                *(output + 2) = (char)*(input + 2);
                *(output + 3) = (char)*(input + 3);
                *(output + 4) = (char)*(input + 4);
                *(output + 5) = (char)*(input + 5);
                *(output + 6) = (char)*(input + 6);
                *(output + 7) = (char)*(input + 7);
                *(output + 8) = (char)*(input + 8);
                *(output + 9) = (char)*(input + 9);
                *(output + 10) = (char)*(input + 10);
                *(output + 11) = (char)*(input + 11);
                output += 12;
                input += 12;
            }
            if (i < count - 5)
            {
                orValue |= *input | *(input + 1) | *(input + 2) | *(input + 3) | *(input + 4) | *(input + 5);
                hasZero = hasZero || *input == 0 || *(input + 1) == 0 || *(input + 2) == 0 || *(input + 3) == 0 ||
                    *(input + 4) == 0 || *(input + 5) == 0;

                i += 6;
                *(output) = (char)*(input);
                *(output + 1) = (char)*(input + 1);
                *(output + 2) = (char)*(input + 2);
                *(output + 3) = (char)*(input + 3);
                *(output + 4) = (char)*(input + 4);
                *(output + 5) = (char)*(input + 5);
                output += 6;
                input += 6;
            }
            if (i < count - 3)
            {
                orValue |= *input | *(input + 1) | *(input + 2) | *(input + 3);
                hasZero = hasZero || *input == 0 || *(input + 1) == 0 || *(input + 2) == 0 || *(input + 3) == 0;

                i += 4;
                *(output) = (char)*(input);
                *(output + 1) = (char)*(input + 1);
                *(output + 2) = (char)*(input + 2);
                *(output + 3) = (char)*(input + 3);
                output += 4;
                input += 4;
            }

            while (i < count)
            {
                orValue |= *input;
                hasZero = hasZero || *input == 0;
                i++;
                *output = (char)*input;
                output++;
                input++;
            }

            return (orValue & 0x80) == 0 && !hasZero;
        }
    }
}
