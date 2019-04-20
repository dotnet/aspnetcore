// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CodeGenerator.HttpUtilities
{
    public class HttpUtilities
    {
        public static string GeneratedFile()
        {
            var httpMethods = new []
            {
                new Tuple<string, String>("CONNECT ", "Connect"),
                new Tuple<string, String>("DELETE ", "Delete"),
                new Tuple<string, String>("HEAD ", "Head"),
                new Tuple<string, String>("PATCH ", "Patch"),
                new Tuple<string, String>("POST ", "Post"),
                new Tuple<string, String>("PUT ", "Put"),
                new Tuple<string, String>("OPTIONS ", "Options"),
                new Tuple<string, String>("TRACE ", "Trace"),
                new Tuple<string, String>("GET ", "Get")
            };

            return GenerateFile(httpMethods);
        }

        private static string GenerateFile(Tuple<string, String>[] httpMethods)
        {
            var maskLength = (byte)Math.Ceiling(Math.Log(httpMethods.Length, 2));

            var methodsInfo = httpMethods.Select(GetMethodStringAndUlongAndMaskLength).ToList();

            var methodsInfoWithoutGet = methodsInfo.Where(m => m.HttpMethod != "Get".ToString()).ToList();

            var methodsAsciiStringAsLong = methodsInfo.Select(m => m.AsciiStringAsLong).ToArray();

            var mask = HttpUtilitiesGeneratorHelpers.SearchKeyByLookThroughMaskCombinations(methodsAsciiStringAsLong, 0, sizeof(ulong) * 8, maskLength);

            if (mask.HasValue == false)
            {
                throw new InvalidOperationException(string.Format("Generated {0} not found.", nameof(mask)));
            }

            var functionGetKnownMethodIndex = GetFunctionBodyGetKnownMethodIndex(mask.Value);

            var methodsSection = GetMethodsSection(methodsInfoWithoutGet);

            var masksSection = GetMasksSection(methodsInfoWithoutGet);

            var setKnownMethodSection = GetSetKnownMethodSection(methodsInfoWithoutGet);
            var methodNamesSection = GetMethodNamesSection(methodsInfo);

            int knownMethodsArrayLength = (int)(Math.Pow(2, maskLength) + 1);
            int methodNamesArrayLength = httpMethods.Length;

            return string.Format(@"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{{
    public static partial class HttpUtilities
    {{
        // readonly primitive statics can be Jit'd to consts https://github.com/dotnet/coreclr/issues/1079
{0}

{1}
        private static readonly Tuple<ulong, ulong, HttpMethod, int>[] _knownMethods =
            new Tuple<ulong, ulong, HttpMethod, int>[{2}];

        private static readonly string[] _methodNames = new string[{3}];

        static HttpUtilities()
        {{
{4}
            FillKnownMethodsGaps();
{5}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetKnownMethodIndex(ulong value)
        {{
{6}
        }}
    }}
}}", methodsSection, masksSection, knownMethodsArrayLength, methodNamesArrayLength, setKnownMethodSection, methodNamesSection, functionGetKnownMethodIndex);
        }

        private static string GetMethodsSection(List<MethodInfo> methodsInfo)
        {
            var result = new StringBuilder();

            for (var index = 0; index < methodsInfo.Count; index++)
            {
                var methodInfo = methodsInfo[index];

                var httpMethodFieldName = GetHttpMethodFieldName(methodInfo);
                result.AppendFormat("        private static readonly ulong {0} = GetAsciiStringAsLong(\"{1}\");", httpMethodFieldName, methodInfo.MethodAsciiString.Replace("\0", "\\0"));

                if (index < methodsInfo.Count - 1)
                {
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        private static string GetMasksSection(List<MethodInfo> methodsInfo)
        {
            var distinctLengths = methodsInfo.Select(m => m.MaskLength).Distinct().ToList();

            distinctLengths.Sort((t1, t2) => -t1.CompareTo(t2));

            var result = new StringBuilder();

            for (var index = 0; index < distinctLengths.Count; index++)
            {
                var maskBytesLength = distinctLengths[index];
                var maskArray = GetMaskArray(maskBytesLength);

                var hexMaskString = HttpUtilitiesGeneratorHelpers.GeHexString(maskArray, "0x", ", ");
                var maskFieldName = GetMaskFieldName(maskBytesLength);

                result.AppendFormat("        private static readonly ulong {0} = GetMaskAsLong(new byte[]\r\n            {{{1}}});", maskFieldName, hexMaskString);
                result.AppendLine();
                if (index < distinctLengths.Count - 1)
                {
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        private static string GetSetKnownMethodSection(List<MethodInfo> methodsInfo)
        {
            methodsInfo = methodsInfo.ToList();

            methodsInfo.Sort((t1, t2) => t1.MaskLength.CompareTo(t2.MaskLength));

            var result = new StringBuilder();

            for (var index = 0; index < methodsInfo.Count; index++)
            {
                var methodInfo = methodsInfo[index];
                var maskFieldName = GetMaskFieldName(methodInfo.MaskLength);
                var httpMethodFieldName = GetHttpMethodFieldName(methodInfo);

                result.AppendFormat("            SetKnownMethod({0}, {1}, HttpMethod.{3}, {4});", maskFieldName, httpMethodFieldName, typeof(String).Name, methodInfo.HttpMethod, methodInfo.MaskLength - 1);

                if (index < methodsInfo.Count - 1)
                {
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        private static string GetMethodNamesSection(List<MethodInfo> methodsInfo)
        {
            methodsInfo = methodsInfo.ToList();

            methodsInfo.Sort((t1, t2) => t1.HttpMethod.CompareTo(t2.HttpMethod));

            var result = new StringBuilder();

            for (var index = 0; index < methodsInfo.Count; index++)
            {
                var methodInfo = methodsInfo[index];

                result.AppendFormat("            _methodNames[(byte)HttpMethod.{1}] = {2}.{3};", typeof(String).Name, methodInfo.HttpMethod, typeof(HttpMethods).Name, methodInfo.HttpMethod);

                if (index < methodsInfo.Count - 1)
                {
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        private static string GetFunctionBodyGetKnownMethodIndex(ulong mask)
        {
            var shifts = HttpUtilitiesGeneratorHelpers.GetShifts(mask);

            var maskHexString = HttpUtilitiesGeneratorHelpers.MaskToHexString(mask);

            string bodyString;

            if (shifts.Length > 0)
            {
                var bitsCount = HttpUtilitiesGeneratorHelpers.CountBits(mask);

                var tmpReturn = string.Empty;
                foreach (var item in shifts)
                {
                    if (tmpReturn.Length > 0)
                    {
                        tmpReturn += " | ";
                    }

                    tmpReturn += string.Format("(tmp >> {1})", HttpUtilitiesGeneratorHelpers.MaskToHexString(item.Mask), item.Shift);
                }

                var mask2 = (ulong)(Math.Pow(2, bitsCount) - 1);

                string returnString = string.Format("return ({0}) & {1};", tmpReturn, HttpUtilitiesGeneratorHelpers.MaskToHexString(mask2));

                bodyString = string.Format("            const int magicNumer = {0};\r\n            var tmp = (int)value & magicNumer;\r\n            {1}", HttpUtilitiesGeneratorHelpers.MaskToHexString(mask), returnString);

            }
            else
            {
                bodyString = string.Format("return (int)(value & {0});", maskHexString);
            }

            return bodyString;
        }

        private static string GetHttpMethodFieldName(MethodInfo methodsInfo)
        {
            return string.Format("_http{0}MethodLong", methodsInfo.HttpMethod.ToString());
        }

        private static string GetMaskFieldName(int nBytes)
        {
            return string.Format("_mask{0}Chars", nBytes);
        }

        private static string GetMethodString(string method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            const int length = sizeof(ulong);

            if (method.Length > length)
            {
                throw new ArgumentException(string.Format("MethodAsciiString {0} length is greather than {1}", method, length));
            }
            string result = method;

            if (result.Length == length)
            {
                return result;
            }

            if (result.Length < length)
            {
                var count = length - result.Length;

                for (int i = 0; i < count; i++)
                {
                    result += "\0";
                }
            }

            return result;
        }

        private class MethodInfo
        {
            public string MethodAsciiString;
            public ulong AsciiStringAsLong;
            public string HttpMethod;
            public int MaskLength;
        }

        private static MethodInfo GetMethodStringAndUlongAndMaskLength(Tuple<string, string> method)
        {
            var methodString = GetMethodString(method.Item1);

            var asciiAsLong = GetAsciiStringAsLong(methodString);

            return new MethodInfo
            {
                MethodAsciiString = methodString,
                AsciiStringAsLong = asciiAsLong,
                HttpMethod = method.Item2.ToString(),
                MaskLength = method.Item1.Length
            };
        }

        private static byte[] GetMaskArray(int n, int length = sizeof(ulong))
        {
            var maskArray = new byte[length];
            for (int i = 0; i < n; i++)
            {
                maskArray[i] = 0xff;
            }
            return maskArray;
        }

        private unsafe static ulong GetAsciiStringAsLong(string str)
        {
            Debug.Assert(str.Length == sizeof(ulong), string.Format("String must be exactly {0} (ASCII) characters long.", sizeof(ulong)));

            var bytes = Encoding.ASCII.GetBytes(str);

            fixed (byte* ptr = &bytes[0])
            {
                return *(ulong*)ptr;
            }
        }
    }
}