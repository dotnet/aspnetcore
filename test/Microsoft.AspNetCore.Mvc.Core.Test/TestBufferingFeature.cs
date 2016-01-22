// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Mvc
{
    internal class TestBufferingFeature : IHttpBufferingFeature
    {
        public bool DisableResponseBufferingInvoked { get; private set; }

        public bool DisableRequestBufferingInvoked { get; private set; }

        public void DisableRequestBuffering()
        {
            DisableRequestBufferingInvoked = true;
        }

        public void DisableResponseBuffering()
        {
            DisableResponseBufferingInvoked = true;
        }
    }
}
