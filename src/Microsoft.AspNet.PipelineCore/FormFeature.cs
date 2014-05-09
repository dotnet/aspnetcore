// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
