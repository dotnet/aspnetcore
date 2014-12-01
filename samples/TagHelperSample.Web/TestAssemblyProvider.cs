// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Mvc;

namespace TagHelperSample.Web
{
    /// <summary>
    /// Limits MVC to use a single Assembly for controller discovery. This is used by the functional test to limit the
    /// Controller discovery to TagHelperSample.Web Assembly alone. The sample should work in the absence of this file
    /// when not run from a functional test.
    /// </summary>
    /// <remarks>
    /// This is a generic type because it needs to instantiated by a service provider to replace a built-in MVC
    /// service.
    /// </remarks>
    public class TestAssemblyProvider<T> : IAssemblyProvider
    {
        public TestAssemblyProvider()
        {
            CandidateAssemblies = new Assembly[] { typeof(T).GetTypeInfo().Assembly };
        }

        public IEnumerable<Assembly> CandidateAssemblies { get; private set; }
    }
}