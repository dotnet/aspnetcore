// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    internal class AsciiUtilities
    {
        public static unsafe void GetAsciiString(byte* input, char* output, int count)
        {
            var i = 0;
            while (i < count - 11)
            {
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
                i++;
                *output = (char)*input;
                output++;
                input++;
            }
        }
    }
}
