// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    public class TextPlainController : Controller
    {
        public Task<string> ReturnTaskOfString()
        {
            return Task.FromResult<string>("ReturnTaskOfString");
        }

        public Task<object> ReturnTaskOfObject_StringValue()
        {
            return Task.FromResult<object>("ReturnTaskOfObject_StringValue");
        }

        public Task<object> ReturnTaskOfObject_ObjectValue()
        {
            return Task.FromResult(new object());
        }

        public string ReturnString()
        {
            return "ReturnString";
        }

        public object ReturnObject_StringValue()
        {
            return "ReturnObject_StringValue";
        }

        public object ReturnObject_ObjectValue()
        {
            return new object();
        }

        public string ReturnString_NullValue()
        {
            return null;
        }

        public object ReturnObject_NullValue()
        {
            return null;
        }
    }
}