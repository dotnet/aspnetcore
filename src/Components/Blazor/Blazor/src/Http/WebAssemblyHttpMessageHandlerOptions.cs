// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Blazor.Http
{
    /// <summary>
    /// Configures options for the WebAssembly HTTP message handler.
    /// </summary>
    public static class WebAssemblyHttpMessageHandlerOptions
    {
        /// <summary>
        /// Gets or sets the default value of the 'credentials' option on outbound HTTP requests.
        /// Defaults to <see cref="FetchCredentialsOption.SameOrigin"/>.
        /// </summary>
        public static FetchCredentialsOption DefaultCredentials
        {
            get
            {
                var valueString = MonoDefaultCredentialsGetter.Value();
                var result = default(FetchCredentialsOption);
                if (valueString != null)
                {
                    Enum.TryParse(valueString, out result);
                }
                return result;
            }

            set
            {
                MonoDefaultCredentialsSetter.Value(value.ToString());
            }
        }

        static Func<Type> MonoWasmHttpMessageHandlerType = ()
            => Assembly.Load("WebAssembly.Net.Http")
                .GetType("WebAssembly.Net.Http.HttpClient.WasmHttpMessageHandler");

        static Func<Type> MonoFetchCredentialsOptionType = ()
            => Assembly.Load("WebAssembly.Net.Http")
                .GetType("WebAssembly.Net.Http.HttpClient.FetchCredentialsOption");

        static Lazy<PropertyInfo> MonoDefaultCredentialsProperty = new Lazy<PropertyInfo>(
            () => MonoWasmHttpMessageHandlerType()?.GetProperty("DefaultCredentials", BindingFlags.Public | BindingFlags.Static));

        static Lazy<Func<string>> MonoDefaultCredentialsGetter = new Lazy<Func<string>>(() =>
        {
            return () => MonoDefaultCredentialsProperty.Value?.GetValue(null).ToString();
        });

        static Lazy<Action<string>> MonoDefaultCredentialsSetter = new Lazy<Action<string>>(() =>
        {
            var fetchCredentialsOptionsType = MonoFetchCredentialsOptionType();
            return value => MonoDefaultCredentialsProperty.Value?.SetValue(null, Enum.Parse(fetchCredentialsOptionsType, value));
        });
    }
}
