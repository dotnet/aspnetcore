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
        internal readonly JsonSerializerOptions _serializerOptions;

        public JsonHubProtocolOptions()
        {
            _serializerOptions = JsonHubProtocol.CreateDefaultSerializerSettings();
        }

        public bool IgnoreNullValues { get => _serializerOptions.IgnoreNullValues; set => _serializerOptions.IgnoreNullValues = value; }
        public bool WriteIndented { get => _serializerOptions.WriteIndented; set => _serializerOptions.WriteIndented = value; }
        public bool AllowTrailingCommas { get => _serializerOptions.AllowTrailingCommas; set => _serializerOptions.AllowTrailingCommas = value; }
    }
}
