// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class UseMiddlewareExtensions
    {
        internal const string InvokeAsyncMethodName = "InvokeAsync";
        internal const string InvokeMethodName = "Invoke";
    }
}

namespace Microsoft.AspNetCore.Http
{
    internal static partial class ParsingHelpers
    {
        public static void AppendHeaderJoined(Microsoft.AspNetCore.Http.IHeaderDictionary headers, string key, params string[] values) { }
        public static void AppendHeaderUnmodified(Microsoft.AspNetCore.Http.IHeaderDictionary headers, string key, Microsoft.Extensions.Primitives.StringValues values) { }
        public static Microsoft.Extensions.Primitives.StringValues GetHeader(Microsoft.AspNetCore.Http.IHeaderDictionary headers, string key) { throw null; }
        public static Microsoft.Extensions.Primitives.StringValues GetHeaderSplit(Microsoft.AspNetCore.Http.IHeaderDictionary headers, string key) { throw null; }
        public static Microsoft.Extensions.Primitives.StringValues GetHeaderUnmodified(Microsoft.AspNetCore.Http.IHeaderDictionary headers, string key) { throw null; }
        public static void SetHeaderJoined(Microsoft.AspNetCore.Http.IHeaderDictionary headers, string key, Microsoft.Extensions.Primitives.StringValues value) { }
        public static void SetHeaderUnmodified(Microsoft.AspNetCore.Http.IHeaderDictionary headers, string key, Microsoft.Extensions.Primitives.StringValues? values) { }
    }
}

namespace Microsoft.AspNetCore.Http.Abstractions
{
    internal static partial class Resources
    {
        internal static string ArgumentCannotBeNullOrEmpty { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string Exception_InvokeDoesNotSupportRefOrOutParams { get { throw null; } }
        internal static string Exception_InvokeMiddlewareNoService { get { throw null; } }
        internal static string Exception_PathMustStartWithSlash { get { throw null; } }
        internal static string Exception_PortMustBeGreaterThanZero { get { throw null; } }
        internal static string Exception_UseMiddleMutlipleInvokes { get { throw null; } }
        internal static string Exception_UseMiddlewareExplicitArgumentsNotSupported { get { throw null; } }
        internal static string Exception_UseMiddlewareIServiceProviderNotAvailable { get { throw null; } }
        internal static string Exception_UseMiddlewareNoInvokeMethod { get { throw null; } }
        internal static string Exception_UseMiddlewareNoMiddlewareFactory { get { throw null; } }
        internal static string Exception_UseMiddlewareNonTaskReturnType { get { throw null; } }
        internal static string Exception_UseMiddlewareNoParameters { get { throw null; } }
        internal static string Exception_UseMiddlewareUnableToCreateMiddleware { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string RouteValueDictionary_DuplicateKey { get { throw null; } }
        internal static string RouteValueDictionary_DuplicatePropertyName { get { throw null; } }
        internal static string FormatException_InvokeDoesNotSupportRefOrOutParams(object p0) { throw null; }
        internal static string FormatException_InvokeMiddlewareNoService(object p0, object p1) { throw null; }
        internal static string FormatException_PathMustStartWithSlash(object p0) { throw null; }
        internal static string FormatException_UseMiddleMutlipleInvokes(object p0, object p1) { throw null; }
        internal static string FormatException_UseMiddlewareExplicitArgumentsNotSupported(object p0) { throw null; }
        internal static string FormatException_UseMiddlewareIServiceProviderNotAvailable(object p0) { throw null; }
        internal static string FormatException_UseMiddlewareNoInvokeMethod(object p0, object p1, object p2) { throw null; }
        internal static string FormatException_UseMiddlewareNoMiddlewareFactory(object p0) { throw null; }
        internal static string FormatException_UseMiddlewareNonTaskReturnType(object p0, object p1, object p2) { throw null; }
        internal static string FormatException_UseMiddlewareNoParameters(object p0, object p1, object p2) { throw null; }
        internal static string FormatException_UseMiddlewareUnableToCreateMiddleware(object p0, object p1) { throw null; }
        internal static string FormatRouteValueDictionary_DuplicateKey(object p0, object p1) { throw null; }
        internal static string FormatRouteValueDictionary_DuplicatePropertyName(object p0, object p1, object p2, object p3) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Routing
{
    public partial class RouteValueDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyDictionary<string, object>, System.Collections.IEnumerable
    {
        internal System.Collections.Generic.KeyValuePair<string, object>[] _arrayStorage;
        internal Microsoft.AspNetCore.Routing.RouteValueDictionary.PropertyStorage _propertyStorage;
        internal partial class PropertyStorage
        {
            public readonly Microsoft.Extensions.Internal.PropertyHelper[] Properties;
            public readonly object Value;
            public PropertyStorage(object value) { }
        }
    }
}

namespace Microsoft.Extensions.Internal
{
    internal partial class PropertyHelper
    {
        public PropertyHelper(System.Reflection.PropertyInfo property) { }
        public virtual string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public System.Reflection.PropertyInfo Property { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<object, object> ValueGetter { get { throw null; } }
        public System.Action<object, object> ValueSetter { get { throw null; } }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Reflection.TypeInfo typeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Type type) { throw null; }
        protected static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Type type, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyHelper> createPropertyHelper, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> cache) { throw null; }
        public object GetValue(object instance) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Reflection.TypeInfo typeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Type type) { throw null; }
        protected static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Type type, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyHelper> createPropertyHelper, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> allPropertiesCache, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> visiblePropertiesCache) { throw null; }
        public static System.Func<object, object> MakeFastPropertyGetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Action<object, object> MakeFastPropertySetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Func<object, object> MakeNullSafeFastPropertyGetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Collections.Generic.IDictionary<string, object> ObjectToDictionary(object value) { throw null; }
        public void SetValue(object instance, object value) { }
    }
}
