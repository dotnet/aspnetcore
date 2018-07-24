// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.Routing
{
    public abstract class LinkGenerator
    {
        public abstract bool TryGetLink(LinkGeneratorContext context, out string link);

        public abstract string GetLink(LinkGeneratorContext context);
    }
}
