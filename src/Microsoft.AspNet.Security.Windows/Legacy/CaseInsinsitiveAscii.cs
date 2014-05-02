// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="CaseInsensitiveAscii.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;

namespace Microsoft.AspNet.Security.Windows
{
    internal class CaseInsensitiveAscii : IEqualityComparer, IComparer
    {
        // ASCII char ToLower table
        internal static readonly byte[] AsciiToLower = new byte[]
        {
              0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
             10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
             20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
             30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
             40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
             50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
             60, 61, 62, 63, 64, 97, 98, 99, 100, 101, // 60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
            102, 103, 104, 105, 106, 107, 108, 109, 110, 111, // 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
            112, 113, 114, 115, 116, 117, 118, 119, 120, 121, // 80, 81, 82, 83, 84, 85, 86, 87, 88, 89,
            122, 91, 92, 93, 94, 95, 96, 97, 98, 99, // 90, 91, 92, 93, 94, 95, 96, 97, 98, 99,
            100, 101, 102, 103, 104, 105, 106, 107, 108, 109,
            110, 111, 112, 113, 114, 115, 116, 117, 118, 119,
            120, 121, 122, 123, 124, 125, 126, 127, 128, 129,
            130, 131, 132, 133, 134, 135, 136, 137, 138, 139,
            140, 141, 142, 143, 144, 145, 146, 147, 148, 149,
            150, 151, 152, 153, 154, 155, 156, 157, 158, 159,
            160, 161, 162, 163, 164, 165, 166, 167, 168, 169,
            170, 171, 172, 173, 174, 175, 176, 177, 178, 179,
            180, 181, 182, 183, 184, 185, 186, 187, 188, 189,
            190, 191, 192, 193, 194, 195, 196, 197, 198, 199,
            200, 201, 202, 203, 204, 205, 206, 207, 208, 209,
            210, 211, 212, 213, 214, 215, 216, 217, 218, 219,
            220, 221, 222, 223, 224, 225, 226, 227, 228, 229,
            230, 231, 232, 233, 234, 235, 236, 237, 238, 239,
            240, 241, 242, 243, 244, 245, 246, 247, 248, 249,
            250, 251, 252, 253, 254, 255
        };

        // ASCII string case insensitive hash function
        public int GetHashCode(object myObject)
        {
            string myString = myObject as string;
            if (myObject == null)
            {
                return 0;
            }
            int myHashCode = myString.Length;
            if (myHashCode == 0)
            {
                return 0;
            }
            myHashCode ^= AsciiToLower[(byte)myString[0]] << 24 ^ AsciiToLower[(byte)myString[myHashCode - 1]] << 16;
            return myHashCode;
        }

        // ASCII string case insensitive comparer
        public int Compare(object firstObject, object secondObject)
        {
            string firstString = firstObject as string;
            string secondString = secondObject as string;
            if (firstString == null)
            {
                return secondString == null ? 0 : -1;
            }
            if (secondString == null)
            {
                return 1;
            }
            int result = firstString.Length - secondString.Length;
            int comparisons = result > 0 ? secondString.Length : firstString.Length;
            int difference, index = 0;
            while (index < comparisons)
            {
                difference = (int)(AsciiToLower[firstString[index]] - AsciiToLower[secondString[index]]);
                if (difference != 0)
                {
                    result = difference;
                    break;
                }
                index++;
            }
            return result;
        }

        // ASCII string case insensitive hash function
        private int FastGetHashCode(string myString)
        {
            int myHashCode = myString.Length;
            if (myHashCode != 0)
            {
                myHashCode ^= AsciiToLower[(byte)myString[0]] << 24 ^ AsciiToLower[(byte)myString[myHashCode - 1]] << 16;
            }
            return myHashCode;
        }

        // ASCII string case insensitive comparer
        public new bool Equals(object firstObject, object secondObject)
        {
            string firstString = firstObject as string;
            string secondString = secondObject as string;
            if (firstString == null)
            {
                return secondString == null;
            }
            if (secondString != null)
            {
                int index = firstString.Length;
                if (index == secondString.Length)
                {
                    if (FastGetHashCode(firstString) == FastGetHashCode(secondString))
                    {
                        int comparisons = firstString.Length;
                        while (index > 0)
                        {
                            index--;
                            if (AsciiToLower[firstString[index]] != AsciiToLower[secondString[index]])
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
