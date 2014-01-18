// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Microsoft.TestCommon
{
    public class MediaTypeHeaderValueComparer : IComparer<MediaTypeHeaderValue>
    {
        private static readonly MediaTypeHeaderValueComparer mediaTypeComparer = new MediaTypeHeaderValueComparer();

        public MediaTypeHeaderValueComparer()
        {
        }

        public static MediaTypeHeaderValueComparer Comparer
        {
            get
            {
                return mediaTypeComparer;
            }
        }

        public int Compare(MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2)
        {
            ParsedMediaTypeHeaderValue parsedMediaType1 = new ParsedMediaTypeHeaderValue(mediaType1);
            ParsedMediaTypeHeaderValue parsedMediaType2 = new ParsedMediaTypeHeaderValue(mediaType2);

            int returnValue = CompareBasedOnQualityFactor(parsedMediaType1, parsedMediaType2);

            if (returnValue == 0)
            {
                if (!String.Equals(parsedMediaType1.Type, parsedMediaType2.Type, StringComparison.OrdinalIgnoreCase))
                {
                    if (parsedMediaType1.IsAllMediaRange)
                    {
                        return 1;
                    }
                    else if (parsedMediaType2.IsAllMediaRange)
                    {
                        return -1;
                    }
                }
                else if (!String.Equals(parsedMediaType1.SubType, parsedMediaType2.SubType, StringComparison.OrdinalIgnoreCase))
                {
                    if (parsedMediaType1.IsSubTypeMediaRange)
                    {
                        return 1;
                    }
                    else if (parsedMediaType2.IsSubTypeMediaRange)
                    {
                        return -1;
                    }
                }
                else
                {
                    if (!parsedMediaType1.HasNonQualityFactorParameter)
                    {
                        if (parsedMediaType2.HasNonQualityFactorParameter)
                        {
                            return 1;
                        }
                    }
                    else if (!parsedMediaType2.HasNonQualityFactorParameter)
                    {
                        return -1;
                    }
                }
            }

            return returnValue;
        }

        private static int CompareBasedOnQualityFactor(ParsedMediaTypeHeaderValue parsedMediaType1, ParsedMediaTypeHeaderValue parsedMediaType2)
        {
            double qualityDifference = parsedMediaType1.QualityFactor - parsedMediaType2.QualityFactor;
            if (qualityDifference < 0)
            {
                return 1;
            }
            else if (qualityDifference > 0)
            {
                return -1;
            }

            return 0;
        }
    }
}