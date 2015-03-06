// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Http
{
    // TODO: Is there any reason not to flatten the Factory down into the Feature?
    public interface ISessionFeature
    {
        ISessionFactory Factory { get; set; }

        ISession Session { get; set; }
    }
}