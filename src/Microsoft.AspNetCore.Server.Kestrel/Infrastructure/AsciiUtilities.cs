// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    internal class AsciiUtilities
    {
        public static unsafe bool TryGetAsciiString(byte* input, char* output, int count)
        {
            var i = 0;
            sbyte* signedInput = (sbyte*)input;

            bool isValid = true;
            while (i < count - 11)
            {
                isValid = isValid && *signedInput > 0 && *(signedInput + 1) > 0 && *(signedInput + 2) > 0 &&
                    *(signedInput + 3) > 0 && *(signedInput + 4) > 0 && *(signedInput + 5) > 0 && *(signedInput + 6) > 0 &&
                    *(signedInput + 7) > 0 && *(signedInput + 8) > 0 && *(signedInput + 9) > 0 && *(signedInput + 10) > 0 &&
                    *(signedInput + 11) > 0;

                i += 12;
                *(output) = (char)*(signedInput);
                *(output + 1) = (char)*(signedInput + 1);
                *(output + 2) = (char)*(signedInput + 2);
                *(output + 3) = (char)*(signedInput + 3);
                *(output + 4) = (char)*(signedInput + 4);
                *(output + 5) = (char)*(signedInput + 5);
                *(output + 6) = (char)*(signedInput + 6);
                *(output + 7) = (char)*(signedInput + 7);
                *(output + 8) = (char)*(signedInput + 8);
                *(output + 9) = (char)*(signedInput + 9);
                *(output + 10) = (char)*(signedInput + 10);
                *(output + 11) = (char)*(signedInput + 11);
                output += 12;
                signedInput += 12;
            }
            if (i < count - 5)
            {
                isValid = isValid && *signedInput > 0 && *(signedInput + 1) > 0 && *(signedInput + 2) > 0 &&
                    *(signedInput + 3) > 0 && *(signedInput + 4) > 0 && *(signedInput + 5) > 0;

                i += 6;
                *(output) = (char)*(signedInput);
                *(output + 1) = (char)*(signedInput + 1);
                *(output + 2) = (char)*(signedInput + 2);
                *(output + 3) = (char)*(signedInput + 3);
                *(output + 4) = (char)*(signedInput + 4);
                *(output + 5) = (char)*(signedInput + 5);
                output += 6;
                signedInput += 6;
            }
            if (i < count - 3)
            {
                isValid = isValid && *signedInput > 0 && *(signedInput + 1) > 0 && *(signedInput + 2) > 0 &&
                    *(signedInput + 3) > 0;

                i += 4;
                *(output) = (char)*(signedInput);
                *(output + 1) = (char)*(signedInput + 1);
                *(output + 2) = (char)*(signedInput + 2);
                *(output + 3) = (char)*(signedInput + 3);
                output += 4;
                signedInput += 4;
            }

            while (i < count)
            {
                isValid = isValid && *signedInput > 0;

                i++;
                *output = (char)*signedInput;
                output++;
                signedInput++;
            }

            return isValid;
        }
    }
}
