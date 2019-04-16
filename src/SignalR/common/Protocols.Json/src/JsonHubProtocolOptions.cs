// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Options used to configure a <see cref="JsonHubProtocolOptions"/> instance.
    /// </summary>
    public class JsonHubProtocolOptions
    {
        /// <summary>
        /// Gets or sets the settings used to serialize invocation arguments and return values.
        /// </summary>
        public CustomJsonOptionsClass PayloadSerializerSettings { get; set; } = JsonHubProtocol.CreateDefaultSerializerSettings();
    }

    public class CustomJsonOptionsClass
    {
        internal readonly JsonSerializerOptions _options;

        public CustomJsonOptionsClass()
        {
            _options = new JsonSerializerOptions();
        }

        public bool IgnoreNullValues { get => _options.IgnoreNullValues; set => _options.IgnoreNullValues = value; }
        public bool WriteIndented { get => _options.WriteIndented; set => _options.WriteIndented = value; }
        public bool AllowTrailingCommas { get => _options.AllowTrailingCommas; set => _options.AllowTrailingCommas = value; }
    }
}
