// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileProviders;

namespace Microsoft.AspNet.Hosting
{
    public interface IHostingEnvironment
    {
        // This must be settable!
        string EnvironmentName { get; set;  }

        // This must be settable!
        string WebRootPath { get; set; }

        // This must be settable!
        IFileProvider WebRootFileProvider { get; set; }
    }
}