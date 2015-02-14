// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Interfaces;

namespace Microsoft.AspNet.Session
{
    public class SessionFeature : ISessionFeature
    {
        public ISessionFactory Factory { get; set; }

        public ISession Session { get; set; }
    }
}