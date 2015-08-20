// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Http.Headers;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http
{
    public static class HeaderDictionaryTypeExtensions
    {
        public static RequestHeaders GetTypedHeaders(this HttpRequest request)
        {
            return new RequestHeaders(request.Headers);
        }

        public static ResponseHeaders GetTypedHeaders(this HttpResponse response)
        {
            return new ResponseHeaders(response.Headers);
        }

        // These are all shared helpers used by both RequestHeaders and ResponseHeaders

        internal static DateTimeOffset? GetDate([NotNull] this IHeaderDictionary headers, [NotNull] string name)
        {
            return headers.Get<DateTimeOffset?>(name);
        }

        internal static void Set([NotNull] this IHeaderDictionary headers, [NotNull] string name, object value)
        {
            if (value == null)
            {
                headers.Remove(name);
            }
            else
            {
                headers[name] = value.ToString();
            }
        }

        internal static void SetList<T>([NotNull] this IHeaderDictionary headers, [NotNull] string name, IList<T> values)
        {
            if (values == null || values.Count == 0)
            {
                headers.Remove(name);
            }
            else
            {
                headers[name] = values.Select(value => value.ToString()).ToArray();
            }
        }

        internal static void SetDate([NotNull] this IHeaderDictionary headers, [NotNull] string name, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                headers[name] = HeaderUtilities.FormatDate(value.Value);
            }
            else
            {
                headers.Remove(name);
            }
        }

        private static IDictionary<Type, object> KnownParsers = new Dictionary<Type, object>()
        {
            { typeof(CacheControlHeaderValue), new Func<string, CacheControlHeaderValue>(value => { CacheControlHeaderValue result; return CacheControlHeaderValue.TryParse(value, out result) ? result : null; }) },
            { typeof(ContentDispositionHeaderValue), new Func<string, ContentDispositionHeaderValue>(value => { ContentDispositionHeaderValue result; return ContentDispositionHeaderValue.TryParse(value, out result) ? result : null; }) },
            { typeof(ContentRangeHeaderValue), new Func<string, ContentRangeHeaderValue>(value => { ContentRangeHeaderValue result; return ContentRangeHeaderValue.TryParse(value, out result) ? result : null; }) },
            { typeof(MediaTypeHeaderValue), new Func<string, MediaTypeHeaderValue>(value => { MediaTypeHeaderValue result; return MediaTypeHeaderValue.TryParse(value, out result) ? result : null; }) },
            { typeof(RangeConditionHeaderValue), new Func<string, RangeConditionHeaderValue>(value => { RangeConditionHeaderValue result; return RangeConditionHeaderValue.TryParse(value, out result) ? result : null; }) },
            { typeof(RangeHeaderValue), new Func<string, RangeHeaderValue>(value => { RangeHeaderValue result; return RangeHeaderValue.TryParse(value, out result) ? result : null; }) },
            { typeof(EntityTagHeaderValue), new Func<string, EntityTagHeaderValue>(value => { EntityTagHeaderValue result; return EntityTagHeaderValue.TryParse(value, out result) ? result : null; }) },
            { typeof(DateTimeOffset?), new Func<string, DateTimeOffset?>(value => { DateTimeOffset result; return HeaderUtilities.TryParseDate(value, out result) ? result : (DateTimeOffset?)null; }) },
            { typeof(long?), new Func<string, long?>(value => { long result; return HeaderUtilities.TryParseInt64(value, out result) ? result : (long?)null; }) },
        };

        private static IDictionary<Type, object> KnownListParsers = new Dictionary<Type, object>()
        {
            { typeof(MediaTypeHeaderValue), new Func<IList<string>, IList<MediaTypeHeaderValue>>(value => { IList<MediaTypeHeaderValue> result; return MediaTypeHeaderValue.TryParseList(value, out result) ? result : null; })  },
            { typeof(StringWithQualityHeaderValue), new Func<IList<string>, IList<StringWithQualityHeaderValue>>(value => { IList<StringWithQualityHeaderValue> result; return StringWithQualityHeaderValue.TryParseList(value, out result) ? result : null; })  },
            { typeof(CookieHeaderValue), new Func<IList<string>, IList<CookieHeaderValue>>(value => { IList<CookieHeaderValue> result; return CookieHeaderValue.TryParseList(value, out result) ? result : null; })  },
            { typeof(EntityTagHeaderValue), new Func<IList<string>, IList<EntityTagHeaderValue>>(value => { IList<EntityTagHeaderValue> result; return EntityTagHeaderValue.TryParseList(value, out result) ? result : null; })  },
            { typeof(SetCookieHeaderValue), new Func<IList<string>, IList<SetCookieHeaderValue>>(value => { IList<SetCookieHeaderValue> result; return SetCookieHeaderValue.TryParseList(value, out result) ? result : null; })  },
        };

        internal static T Get<T>([NotNull] this IHeaderDictionary headers, string name)
        {
            object temp;
            if (KnownParsers.TryGetValue(typeof(T), out temp))
            {
                var func = (Func<string, T>)temp;
                return func(headers[name]);
            }

            var value = headers[name];
            if (StringValues.IsNullOrEmpty(value))
            {
                return default(T);
            }

            return GetViaReflection<T>(value);
        }

        internal static IList<T> GetList<T>([NotNull] this IHeaderDictionary headers, string name)
        {
            object temp;
            if (KnownListParsers.TryGetValue(typeof(T), out temp))
            {
                var func = (Func<IList<string>, IList<T>>)temp;
                return func(headers[name]);
            }

            var values = headers[name];
            if (StringValues.IsNullOrEmpty(values))
            {
                return null;
            }

            return GetListViaReflection<T>(values);
        }

        private static T GetViaReflection<T>(string value)
        {
            // TODO: Cache the reflected type for later? Only if success?
            var type = typeof(T);
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(methodInfo =>
                {
                    if (string.Equals("TryParse", methodInfo.Name, StringComparison.Ordinal)
                        && methodInfo.ReturnParameter.ParameterType.Equals(typeof(bool)))
                    {
                        var methodParams = methodInfo.GetParameters();
                        return methodParams.Length == 2
                            && methodParams[0].ParameterType.Equals(typeof(string))
                            && methodParams[1].IsOut
                            && methodParams[1].ParameterType.Equals(type.MakeByRefType());
                    }
                    return false;
                }).FirstOrDefault();

            if (method == null)
            {
                throw new NotSupportedException(string.Format(
                    "The given type '{0}' does not have a TryParse method with the required signature 'public static bool TryParse(string, out {0}).", nameof(T)));
            }

            var parameters = new object[] { value, null };
            var success = (bool)method.Invoke(null, parameters);
            if (success)
            {
                return (T)parameters[1];
            }
            return default(T);
        }

        private static IList<T> GetListViaReflection<T>(StringValues values)
        {
            // TODO: Cache the reflected type for later? Only if success?
            var type = typeof(T);
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(methodInfo =>
                {
                    if (string.Equals("TryParseList", methodInfo.Name, StringComparison.Ordinal)
                        && methodInfo.ReturnParameter.ParameterType.Equals(typeof(Boolean)))
                    {
                            var methodParams = methodInfo.GetParameters();
                            return methodParams.Length == 2
                                && methodParams[0].ParameterType.Equals(typeof(IList<string>))
                                && methodParams[1].IsOut
                                && methodParams[1].ParameterType.Equals(typeof(IList<T>).MakeByRefType());
                    }
                    return false;
                }).FirstOrDefault();

            if (method == null)
            {
                throw new NotSupportedException(string.Format(
                    "The given type '{0}' does not have a TryParseList method with the required signature 'public static bool TryParseList(IList<string>, out IList<{0}>).", nameof(T)));
            }

            var parameters = new object[] { values, null };
            var success = (bool)method.Invoke(null, parameters);
            if (success)
            {
                return (IList<T>)parameters[1];
            }
            return null;
        }
    }
}