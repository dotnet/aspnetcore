// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    // TODO: Is there any reason not to flatten the Factory down into the Feature?
    [AssemblyNeutral]
    public interface ISessionFeature
    {
        ISessionFactory Factory { get; set; }

        ISession Session { get; set; }
    }
}