// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features
{
    public class ResponseBodyPipeFeature : IResponseBodyPipeFeature
    {
        private readonly static Func<IFeatureCollection, IHttpResponseFeature> _nullRequestFeature = f => null;

        private PipeWriter _pipeWriter;
        private FeatureReferences<IHttpResponseFeature> _features;

        public ResponseBodyPipeFeature(IFeatureCollection features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = new FeatureReferences<IHttpResponseFeature>(features);
        }

        private IHttpResponseFeature HttpResponseFeature =>
            _features.Fetch(ref _features.Cache, _nullRequestFeature);

        public PipeWriter ResponseBodyPipe
        {
            get
            {
                if (_pipeWriter == null ||
                    // If the Response.Body has been updated, recreate the pipeWriter
                    (_pipeWriter is StreamPipeWriter writer && !object.ReferenceEquals(writer.InnerStream, HttpResponseFeature.Body)))
                {
                    _pipeWriter = new StreamPipeWriter(HttpResponseFeature.Body);
                }

                return _pipeWriter;
            }
            set
            {
                _pipeWriter = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}
