// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Internal
{
    internal static partial class RangeHelper
    {
        internal static Microsoft.Net.Http.Headers.RangeItemHeaderValue NormalizeRange(Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long length) { throw null; }
        public static (bool isRangeRequest, Microsoft.Net.Http.Headers.RangeItemHeaderValue range) ParseRange(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.Headers.RequestHeaders requestHeaders, long length, Microsoft.Extensions.Logging.ILogger logger) { throw null; }
    }
}

namespace Microsoft.AspNetCore.StaticFiles
{
    public partial class StaticFileMiddleware
    {
        internal static bool LookupContentType(Microsoft.AspNetCore.StaticFiles.IContentTypeProvider contentTypeProvider, Microsoft.AspNetCore.Builder.StaticFileOptions options, Microsoft.AspNetCore.Http.PathString subPath, out string contentType) { throw null; }
        internal static bool ValidatePath(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.PathString matchUrl, out Microsoft.AspNetCore.Http.PathString subPath) { throw null; }
    }
}
