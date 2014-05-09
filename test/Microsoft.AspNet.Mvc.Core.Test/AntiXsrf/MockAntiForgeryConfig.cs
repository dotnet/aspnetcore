// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public sealed class MockAntiForgeryConfig : IAntiForgeryConfig
    {
        public string CookieName
        {
            get;
            set;
        }

        public string FormFieldName
        {
            get;
            set;
        }

        public bool RequireSSL
        {
            get;
            set;
        }

        public bool SuppressXFrameOptionsHeader
        {
            get;
            set;
        }
    }
}