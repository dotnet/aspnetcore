// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Http;
using Microsoft.AspNet.Mvc.WebApiCompatShim;

namespace WebApiCompatShimWebSite
{
    // This was ported from the WebAPI 5.2 codebase. Kept the same intentionally for compatability.
    [ActionSelectionFilter]
    public class EnumParameterOverloadsController : ApiController
    {
        public IEnumerable<string> Get()
        {
            return new string[] { "get" };
        }

        public string GetWithEnumParameter(UserKind scope)
        {
            return scope.ToString();
        }

        public string GetWithTwoEnumParameters([FromUri]UserKind level, UserKind kind)
        {
            return level.ToString() + kind.ToString();
        }

        public string GetWithNullableEnumParameter(TraceLevel? level)
        {
            return level.ToString();
        }
    }
}