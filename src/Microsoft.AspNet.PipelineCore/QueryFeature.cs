// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class QueryFeature : IQueryFeature
    {
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;
        private string _queryString;
        private IReadableStringCollection _query;

        public QueryFeature(IFeatureCollection features)
        {
            _features = features;
        }

        public IReadableStringCollection Query
        {
            get
            {
                var queryString = _request.Fetch(_features).QueryString;
                if (_query == null || _queryString != queryString)
                {
                    _queryString = queryString;
                    _query = new ReadableStringCollection(ParsingHelpers.GetQuery(queryString));
                }
                return _query;
            }
        }
    }
}