// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
{
    public class MediaTypeHeaderValue
    {
        public string Charset { get; set; }

        public string MediaType { get; set; }

        public string MediaSubType { get; set; }

        public MediaTypeHeaderValueRange MediaTypeRange { get; set; }

        public string RawValue
        {
            get
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(MediaType);
                stringBuilder.Append('/');
                stringBuilder.Append(MediaSubType);
                if (!string.IsNullOrEmpty(Charset))
                {
                    stringBuilder.Append(";charset=");
                    stringBuilder.Append(Charset);
                }

                foreach (var parameter in Parameters)
                {
                    if (string.Equals(parameter.Key, "charset", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    stringBuilder.Append(";");
                    stringBuilder.Append(parameter.Key);
                    stringBuilder.Append("=");
                    stringBuilder.Append(parameter.Value);
                }

                return stringBuilder.ToString();
            }
        }

        public IDictionary<string, string> Parameters { get; set; }

        public static MediaTypeHeaderValue Parse(string input)
        {
            MediaTypeHeaderValue headerValue = null;
            if (!TryParse(input, out headerValue))
            {
                throw new ArgumentException(Resources.FormatInvalidContentType(input));
            }

            return headerValue;
        }

        public static bool TryParse(string input, out MediaTypeHeaderValue headerValue)
        {
            headerValue = null;
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            var inputArray = input.Split(new[] { ';' }, 2);
            var mediaTypeParts = inputArray[0].Split('/');
            if (mediaTypeParts.Length != 2)
            {
                return false;
            }

            // TODO: throw if the media type and subtypes are invalid.
            var mediaType = mediaTypeParts[0].Trim();
            var mediaSubType = mediaTypeParts[1].Trim();
            var mediaTypeRange = MediaTypeHeaderValueRange.None;
            if (mediaType == "*" && mediaSubType == "*")
            {
                mediaTypeRange = MediaTypeHeaderValueRange.AllMediaRange;
            }
            else if (mediaSubType == "*")
            {
                mediaTypeRange = MediaTypeHeaderValueRange.SubtypeMediaRange;
            }

            Dictionary<string, string> parameters = null;
            string charset = null;
            if (inputArray.Length == 2)
            {
                parameters = ParseParameters(inputArray[1]);
                parameters.TryGetValue("charset", out charset);
            }

            headerValue = new MediaTypeHeaderValue()
            {
                MediaType = mediaType,
                MediaSubType = mediaSubType,
                MediaTypeRange = mediaTypeRange,
                Charset = charset,
                Parameters = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            };

            return true;
        }

        protected static Dictionary<string, string> ParseParameters(string inputString)
        {
            var acceptParameters = inputString.Split(';');
            var parameterNameValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in acceptParameters)
            {
                var index = parameter.Split('=');
                if (index.Length == 2)
                {
                    // TODO: throw exception if this is not the case.
                    parameterNameValue.Add(index[0].Trim(), index[1].Trim());
                }
            }

            return parameterNameValue;
        }

        /// <summary>
        /// Determines whether this instance is a subset of passed <see cref="MediaTypeHeaderValue"/>.
        /// If the media type and media type parameters of this media type are all present 
        /// and match those of <paramref name="otherMediaType"/> then it is a match even though 
        /// <paramref name="otherMediaType"/> may have additional parameters.
        /// </summary>
        /// <param name="mediaType">The first media type.</param>
        /// <param name="otherMediaType">The second media type.</param>
        /// <returns><c>true</c> if this is a subset of <paramref name="otherMediaType"/>; false otherwise.</returns>
        public bool IsSubsetOf(MediaTypeHeaderValue otherMediaType)
        {
            if (otherMediaType == null)
            {
                return false;
            }

            if (!MediaType.Equals(otherMediaType.MediaType, StringComparison.OrdinalIgnoreCase))
            {
                if (otherMediaType.MediaTypeRange != MediaTypeHeaderValueRange.AllMediaRange)
                {
                    return false;
                }
            }
            else if (!MediaSubType.Equals(otherMediaType.MediaSubType, StringComparison.OrdinalIgnoreCase))
            {
                if (otherMediaType.MediaTypeRange != MediaTypeHeaderValueRange.SubtypeMediaRange)
                {
                    return false;
                }
            }

            if (Parameters != null)
            {
                if (Parameters.Count != 0 &&
                    (otherMediaType.Parameters == null || otherMediaType.Parameters.Count == 0))
                {
                    return false;
                }

                // So far we either have a full match or a subset match. Now check that all of 
                // mediaType1's parameters are present and equal in mediatype2
                if (!MatchParameters(Parameters, otherMediaType.Parameters))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchParameters(IDictionary<string, string> parameters1,
                                            IDictionary<string, string> parameters2)
        {
            foreach (var parameterKey in parameters1.Keys)
            {
                string parameterValue2 = null;
                if (!parameters2.TryGetValue(parameterKey, out parameterValue2))
                {
                    return false;
                }

                if (parameterValue2 == null || !parameterValue2.Equals(parameters1[parameterKey]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
