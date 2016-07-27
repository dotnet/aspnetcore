// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;

namespace Microsoft.AspNetCore.Server.WebListener
{
    public class WebListenerOptions
    {
        internal static readonly int DefaultMaxAccepts = 5 * Environment.ProcessorCount;

        public Microsoft.Net.Http.Server.WebListener Listener { get; set; } = new Microsoft.Net.Http.Server.WebListener();

        public int MaxAccepts { get; set; } = DefaultMaxAccepts;

        public bool EnableResponseCaching { get; set; } = true;
    }
}
