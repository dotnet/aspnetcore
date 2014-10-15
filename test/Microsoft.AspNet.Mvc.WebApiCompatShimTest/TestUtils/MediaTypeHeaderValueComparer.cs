// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        internal class ParsedMediaTypeHeaderValue
        {
            private const string MediaRangeAsterisk = "*";
            private const char MediaTypeSubTypeDelimiter = '/';
            private const string QualityFactorParameterName = "q";
            private const double DefaultQualityFactor = 1.0;

            private MediaTypeHeaderValue mediaType;
            private string type;
            private string subType;
            private bool? hasNonQualityFactorParameter;
            private double? qualityFactor;

            public ParsedMediaTypeHeaderValue(MediaTypeHeaderValue mediaType)
            {
                this.mediaType = mediaType;
                string[] splitMediaType = mediaType.MediaType.Split(MediaTypeSubTypeDelimiter);
                this.type = splitMediaType[0];
                this.subType = splitMediaType[1];
            }

            public string Type
            {
                get
                {
                    return this.type;
                }
            }

            public string SubType
            {
                get
                {
                    return this.subType;
                }
            }

            public bool IsAllMediaRange
            {
                get
                {
                    return this.IsSubTypeMediaRange && String.Equals(MediaRangeAsterisk, this.Type, StringComparison.Ordinal);
                }
            }

            public bool IsSubTypeMediaRange
            {
                get
                {
                    return String.Equals(MediaRangeAsterisk, this.SubType, StringComparison.Ordinal);
                }
            }

            public bool HasNonQualityFactorParameter
            {
                get
                {
                    if (!this.hasNonQualityFactorParameter.HasValue)
                    {
                        this.hasNonQualityFactorParameter = false;
                        foreach (NameValueHeaderValue param in this.mediaType.Parameters)
                        {
                            if (!String.Equals(QualityFactorParameterName, param.Name, StringComparison.Ordinal))
                            {
                                this.hasNonQualityFactorParameter = true;
                            }
                        }
                    }

                    return this.hasNonQualityFactorParameter.Value;
                }
            }

            public string CharSet
            {
                get
                {
                    return this.mediaType.CharSet;
                }
            }

            public double QualityFactor
            {
                get
                {
                    if (!this.qualityFactor.HasValue)
                    {
                        MediaTypeWithQualityHeaderValue mediaTypeWithQuality = this.mediaType as MediaTypeWithQualityHeaderValue;
                        if (mediaTypeWithQuality != null)
                        {
                            this.qualityFactor = mediaTypeWithQuality.Quality;
                        }

                        if (!this.qualityFactor.HasValue)
                        {
                            this.qualityFactor = DefaultQualityFactor;
                        }
                    }

                    return this.qualityFactor.Value;
                }
            }
        }
    }
}