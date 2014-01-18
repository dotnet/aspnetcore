// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// Unit test utility for testing <see cref="HttpResponseMessage"/> instances.
    /// </summary>
    public class HttpAssert
    {
        private const string CommaSeperator = ", ";
        private static readonly HttpAssert singleton = new HttpAssert();

        public static HttpAssert Singleton { get { return singleton; } }

        /// <summary>
        /// Asserts that the expected <see cref="HttpRequestMessage"/> is equal to the actual <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="expected">The expected <see cref="HttpRequestMessage"/>. Should not be <c>null</c>.</param>
        /// <param name="actual">The actual <see cref="HttpRequestMessage"/>. Should not be <c>null</c>.</param>
        public void Equal(HttpRequestMessage expected, HttpRequestMessage actual)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            Assert.Equal(expected.Version, actual.Version);
            Equal(expected.Headers, actual.Headers);

            if (expected.Content == null)
            {
                Assert.Null(actual.Content);
            }
            else
            {
                string expectedContent = CleanContentString(expected.Content.ReadAsStringAsync().Result);
                string actualContent = CleanContentString(actual.Content.ReadAsStringAsync().Result);
                Assert.Equal(expectedContent, actualContent);
                Equal(expected.Content.Headers, actual.Content.Headers);
            }
        }

        /// <summary>
        /// Asserts that the expected <see cref="HttpResponseMessage"/> is equal to the actual <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="expected">The expected <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        /// <param name="actual">The actual <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        public void Equal(HttpResponseMessage expected, HttpResponseMessage actual)
        {
            Equal(expected, actual, null);
        }

        /// <summary>
        /// Asserts that the expected <see cref="HttpResponseMessage"/> is equal to the actual <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="expected">The expected <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        /// <param name="actual">The actual <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        /// <param name="verifyContentCallback">The callback to verify the Content string. If it is null, Assert.Equal will be used. </param>
        public void Equal(HttpResponseMessage expected, HttpResponseMessage actual, Action<string, string> verifyContentStringCallback)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.ReasonPhrase, actual.ReasonPhrase);
            Assert.Equal(expected.Version, actual.Version);
            Equal(expected.Headers, actual.Headers);

            if (expected.Content == null)
            {
                Assert.Null(actual.Content);
            }
            else
            {
                string expectedContent = CleanContentString(expected.Content.ReadAsStringAsync().Result);
                string actualContent = CleanContentString(actual.Content.ReadAsStringAsync().Result);
                if (verifyContentStringCallback != null)
                {
                    verifyContentStringCallback(expectedContent, actualContent);
                }
                else
                {
                    Assert.Equal(expectedContent, actualContent);
                }
                Equal(expected.Content.Headers, actual.Content.Headers);
            }
        }

        /// <summary>
        /// Asserts that the expected <see cref="HttpHeaders"/> instance is equal to the actual <see cref="actualHeaders"/> instance.
        /// </summary>
        /// <param name="expectedHeaders">The expected <see cref="HttpHeaders"/> instance. Should not be <c>null</c>.</param>
        /// <param name="actualHeaders">The actual <see cref="HttpHeaders"/> instance. Should not be <c>null</c>.</param>
        public void Equal(HttpHeaders expectedHeaders, HttpHeaders actualHeaders)
        {
            Assert.NotNull(expectedHeaders);
            Assert.NotNull(actualHeaders);

            Assert.Equal(expectedHeaders.Count(), actualHeaders.Count());

            foreach (KeyValuePair<string, IEnumerable<string>> expectedHeader in expectedHeaders)
            {
                KeyValuePair<string, IEnumerable<string>> actualHeader = actualHeaders.FirstOrDefault(h => h.Key == expectedHeader.Key);
                Assert.NotNull(actualHeader);

                if (expectedHeader.Key == "Date")
                {
                    HandleDateHeader(expectedHeader.Value.ToArray(), actualHeader.Value.ToArray());
                }
                else
                {
                    string expectedHeaderStr = string.Join(CommaSeperator, expectedHeader.Value);
                    string actualHeaderStr = string.Join(CommaSeperator, actualHeader.Value);
                    Assert.Equal(expectedHeaderStr, actualHeaderStr);
                }
            }
        }

        /// <summary>
        /// Asserts the given <see cref="HttpHeaders"/> contain the given <paramref name="values"/>
        /// for the given <paramref name="name"/>.
        /// </summary>
        /// <param name="headers">The <see cref="HttpHeaders"/> to examine.  It cannot be <c>null</c>.</param>
        /// <param name="name">The name of the header.  It cannot be empty.</param>
        /// <param name="values">The values that must all be present.  It cannot be null.</param>
        public void Contains(HttpHeaders headers, string name, params string[] values)
        {
            Assert.NotNull(headers);
            Assert.False(String.IsNullOrWhiteSpace(name), "Test error: name cannot be empty.");
            Assert.NotNull(values);

            IEnumerable<string> headerValues = null;
            bool foundIt = headers.TryGetValues(name, out headerValues);
            Assert.True(foundIt);

            foreach (string value in values)
            {
                Assert.Contains(value, headerValues);
            }
        }

        public bool IsKnownUnserializableType(Type type, Func<Type, bool> isTypeUnserializableCallback)
        {
            if (isTypeUnserializableCallback != null && isTypeUnserializableCallback(type))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (type.GetMethod("Add") == null)
                    {
                        return true;
                    }
                }

                // Generic type -- recursively analyze generic arguments
                return IsKnownUnserializableType(type.GetGenericArguments()[0], isTypeUnserializableCallback);
            }

            if (type.HasElementType && IsKnownUnserializableType(type.GetElementType(), isTypeUnserializableCallback))
            {
                return true;
            }

            return false;
        }

        public bool IsKnownUnserializable(Type type, object obj, Func<Type, bool> isTypeUnserializableCallback)
        {
            if (IsKnownUnserializableType(type, isTypeUnserializableCallback))
            {
                return true;
            }

            return obj != null && IsKnownUnserializableType(obj.GetType(), isTypeUnserializableCallback);
        }

        public bool IsKnownUnserializable(Type type, object obj)
        {
            return IsKnownUnserializable(type, obj, null);
        }

        public bool CanRoundTrip(Type type)
        {
            if (typeof(DateTime).IsAssignableFrom(type))
            {
                return false;
            }

            if (typeof(DateTimeOffset).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.IsGenericType)
            {
                foreach (Type genericParameterType in type.GetGenericArguments())
                {
                    if (!CanRoundTrip(genericParameterType))
                    {
                        return false;
                    }
                }
            }

            if (type.HasElementType)
            {
                return CanRoundTrip(type.GetElementType());
            }

            return true;
        }

        private static void HandleDateHeader(string[] expectedDateHeaderValues, string[] actualDateHeaderValues)
        {
            Assert.Equal(expectedDateHeaderValues.Length, actualDateHeaderValues.Length);

            for (int i = 0; i < expectedDateHeaderValues.Length; i++)
            {
                DateTime expectedDateTime = DateTime.Parse(expectedDateHeaderValues[i]);
                DateTime actualDateTime = DateTime.Parse(actualDateHeaderValues[i]);

                Assert.Equal(expectedDateTime.Year, actualDateTime.Year);
                Assert.Equal(expectedDateTime.Month, actualDateTime.Month);
                Assert.Equal(expectedDateTime.Day, actualDateTime.Day);

                int hourDifference = Math.Abs(actualDateTime.Hour - expectedDateTime.Hour);
                Assert.True(hourDifference <= 1);

                int minuteDifference = Math.Abs(actualDateTime.Minute - expectedDateTime.Minute);
                Assert.True(minuteDifference <= 1);
            }
        }

        private static string CleanContentString(string content)
        {
            Assert.Null(content);

            string cleanedContent = null;

            // remove any port numbers from Uri's
            cleanedContent = Regex.Replace(content, ":\\d+", "");

            return cleanedContent;
        }
    }
}
