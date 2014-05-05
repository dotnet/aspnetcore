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
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Infrastructure;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class RequestCookiesFeature : IRequestCookiesFeature
    {
        private readonly IFeatureCollection _features;
        private readonly FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;
        private string _cookiesHeader;
        private RequestCookiesCollection _cookiesCollection;
        private static readonly string[] ZeroHeaders = new string[0];

        public RequestCookiesFeature(IFeatureCollection features)
        {
            _features = features;
        }

        public IReadableStringCollection Cookies
        {
            get
            {
                var headers = _request.Fetch(_features).Headers;
                string cookiesHeader = ParsingHelpers.GetHeader(headers, Constants.Headers.Cookie) ?? "";

                if (_cookiesCollection == null)
                {
                    _cookiesCollection = new RequestCookiesCollection();
                    _cookiesCollection.Reparse(cookiesHeader);
                    _cookiesHeader = cookiesHeader;
                }
                else if (!string.Equals(_cookiesHeader, cookiesHeader, StringComparison.Ordinal))
                {
                    _cookiesCollection.Reparse(cookiesHeader);
                    _cookiesHeader = cookiesHeader;
                }

                return _cookiesCollection;
            }
        }
    }
}