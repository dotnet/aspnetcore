// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features
{
    public class RequestBodyPipeFeature : IRequestBodyPipeFeature
    {
        // Lambda hoisted to static readonly field to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IHttpRequestFeature> _nullRequestFeature = f => null;

        private PipeReader _pipeReader;
        private FeatureReferences<IHttpRequestFeature> _features;

        public RequestBodyPipeFeature(IFeatureCollection features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = new FeatureReferences<IHttpRequestFeature>(features);
        }

        private IHttpRequestFeature HttpRequestFeature =>
            _features.Fetch(ref _features.Cache, _nullRequestFeature);

        public PipeReader RequestBodyPipe
        {
            get
            {
                if (_pipeReader == null ||
                    (_pipeReader is StreamPipeReader reader && !object.ReferenceEquals(reader.InnerStream, HttpRequestFeature.Body)))
                {
                    _pipeReader = new StreamPipeReader(HttpRequestFeature.Body);
                }

                return _pipeReader;
            }
            set
            {
                _pipeReader = value ?? throw new ArgumentNullException(nameof(value));
                // TODO set the request body Stream to an adapted pipe https://github.com/aspnet/AspNetCore/issues/3971
            }
        }
    }
}
