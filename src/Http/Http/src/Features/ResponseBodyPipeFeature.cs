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

        public PipeWriter PipeWriter
        {
            get
            {
                if (_pipeWriter == null)
                {
                    _pipeWriter = new StreamPipeWriter(HttpResponseFeature.Body);
                }

                return _pipeWriter;
            }
            set
            {
                _pipeWriter = value;
                if (_pipeWriter == null)
                {
                    HttpResponseFeature.Body = Stream.Null;
                }
                else
                {
                    // TODO set the Response body to adapted pipe https://github.com/aspnet/AspNetCore/issues/3971
                }
            }
        }
    }
}
