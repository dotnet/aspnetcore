// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
{
    public class MediaTypeWithQualityHeaderValue : MediaTypeHeaderValue
    {
        public double? Quality { get; private set; }

        public static new MediaTypeWithQualityHeaderValue Parse(string input)
        {
            MediaTypeHeaderValue mediaTypeHeaderValue;
            if (!MediaTypeHeaderValue.TryParse(input, out mediaTypeHeaderValue))
            {
                return null;
            }

            var quality = FormattingUtilities.Match;
            string qualityStringValue = null;
            if (mediaTypeHeaderValue.Parameters.TryGetValue("q", out qualityStringValue))
            {
                if (!Double.TryParse(qualityStringValue, out quality))
                {
                    return null;
                }
            }

            return
                new MediaTypeWithQualityHeaderValue()
                {
                    MediaType = mediaTypeHeaderValue.MediaType,
                    MediaSubType = mediaTypeHeaderValue.MediaSubType,
                    MediaTypeRange = mediaTypeHeaderValue.MediaTypeRange,
                    Charset = mediaTypeHeaderValue.Charset,
                    Parameters = mediaTypeHeaderValue.Parameters,
                    Quality = quality,
                };
        } 
    }
}
