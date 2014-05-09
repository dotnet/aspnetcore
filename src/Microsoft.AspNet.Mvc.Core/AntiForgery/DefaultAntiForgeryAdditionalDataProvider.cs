// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultAntiForgeryAdditionalDataProvider : IAntiForgeryAdditionalDataProvider
    {
        public virtual string GetAdditionalData(HttpContext context)
        {
            return string.Empty;
        }

        public virtual bool ValidateAdditionalData(HttpContext context, string additionalData)
        {
            // Default implementation does not understand anything but empty data.
            return string.IsNullOrEmpty(additionalData);
        }
    }
}