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

namespace Microsoft.AspNet.Mvc
{
    // Provides configuration information about the anti-forgery system.
    public interface IAntiForgeryConfig
    {
        // Name of the cookie to use.
        string CookieName { get; }

        // Name of the form field to use.
        string FormFieldName { get; }

        // Whether SSL is mandatory for this request.
        bool RequireSSL { get; }

        // Skip X-FRAME-OPTIONS header.
        bool SuppressXFrameOptionsHeader { get; }
    }
}