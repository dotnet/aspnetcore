// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
{
    public class StringWithQualityHeaderValue
    {
        public double? Quality { get; set; }

        public string RawValue { get; set; }

        public string Value { get; set; }

        public static StringWithQualityHeaderValue Parse(string input)
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
                    if (!Double.TryParse(nameValuePair[1].Trim(), out quality))
                    {
                        return null;
                    }
                }
            }

            var stringWithQualityHeader = new StringWithQualityHeaderValue()
            {
                Quality = quality,
                Value = value,
                RawValue = input
            };

            return stringWithQualityHeader;
        }
    }
}
