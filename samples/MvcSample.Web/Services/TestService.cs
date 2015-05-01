// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace MvcSample.Web.Services
{
    public interface ITestService
    {
        string GetFoo();
    }


    public class TestService : ITestService
    {
        public string GetFoo()
        {
            return "Hello world " + DateTime.UtcNow;
        }
    }
}