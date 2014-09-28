// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
{
    public class StringWithQualityHeaderValue
    {
        public double? Quality { get; set; }

        public string RawValue { get; set; }

        public string Value { get; set; }

        public static bool TryParse(string input, out StringWithQualityHeaderValue headerValue)
        {
            var inputArray = input.Split(new[] { ';' }, 2);
            var value = inputArray[0].Trim();

            // Unspecified q factor value is equal to a match.
            var quality = HttpHeaderUtilitites.Match;
            if (inputArray.Length > 1)
            {
                var parameter = inputArray[1].Trim();
                var nameValuePair = parameter.Split(new[] { '=' }, 2);
                if (nameValuePair.Length > 1 && nameValuePair[0].Trim().Equals("q"))
                {
                    // TODO: all extraneous parameters are ignored. Throw/return null if that is the case.
                    if (!double.TryParse(
                            nameValuePair[1],
                            NumberStyles.AllowLeadingWhite | NumberStyles.AllowDecimalPoint |
                                NumberStyles.AllowTrailingWhite,
                            NumberFormatInfo.InvariantInfo,
                            out quality))
                    {
                        headerValue = null;
                        return false;
                    }
                }
            }

            var stringWithQualityHeader = new StringWithQualityHeaderValue()
            {
                Quality = quality,
                Value = value,
                RawValue = input
            };

            headerValue = stringWithQualityHeader;
            return true;
        }

        public static StringWithQualityHeaderValue Parse(string input)
        {
            StringWithQualityHeaderValue headerValue;
            if(!TryParse(input, out headerValue))
            {
                throw new ArgumentException(Resources.FormatInvalidAcceptCharset(input));
            }

            return headerValue;
        }
    }
}
