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

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class FormFeature : IFormFeature
    {
        private readonly IFeatureCollection _features;
        private readonly FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;
        private Stream _bodyStream;
        private IReadableStringCollection _form;

        public FormFeature(IFeatureCollection features)
        {
            _features = features;
        }

        public async Task<IReadableStringCollection> GetFormAsync()
        {
            var body = _request.Fetch(_features).Body;

            if (_bodyStream == null || _bodyStream != body)
            {
                _bodyStream = body;
                using (var streamReader = new StreamReader(body, Encoding.UTF8,
                                                           detectEncodingFromByteOrderMarks: true,
                                                           bufferSize: 1024, leaveOpen: true))
                {
                    string formQuery = await streamReader.ReadToEndAsync();
                    _form = new ReadableStringCollection(ParsingHelpers.GetQuery(formQuery));
                }
            }
            return _form;
        }
    }
}
