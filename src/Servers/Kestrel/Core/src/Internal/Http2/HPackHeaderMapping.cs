// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal static class HPackHeaderMapping
    {
        public static int GetResponseHeaderStaticTableId(KnownHeaderType responseHeaderType)
        {
            switch (responseHeaderType)
            {
                case KnownHeaderType.CacheControl:
                    return H2StaticTable.CacheControl;
                case KnownHeaderType.Date:
                    return H2StaticTable.Date;
                case KnownHeaderType.TransferEncoding:
                    return H2StaticTable.TransferEncoding;
                case KnownHeaderType.Via:
                    return H2StaticTable.Via;
                case KnownHeaderType.Allow:
                    return H2StaticTable.Allow;
                case KnownHeaderType.ContentType:
                    return H2StaticTable.ContentType;
                case KnownHeaderType.ContentEncoding:
                    return H2StaticTable.ContentEncoding;
                case KnownHeaderType.ContentLanguage:
                    return H2StaticTable.ContentLanguage;
                case KnownHeaderType.ContentLocation:
                    return H2StaticTable.ContentLocation;
                case KnownHeaderType.ContentRange:
                    return H2StaticTable.ContentRange;
                case KnownHeaderType.Expires:
                    return H2StaticTable.Expires;
                case KnownHeaderType.LastModified:
                    return H2StaticTable.LastModified;
                case KnownHeaderType.AcceptRanges:
                    return H2StaticTable.AcceptRanges;
                case KnownHeaderType.Age:
                    return H2StaticTable.Age;
                case KnownHeaderType.ETag:
                    return H2StaticTable.ETag;
                case KnownHeaderType.Location:
                    return H2StaticTable.Location;
                case KnownHeaderType.ProxyAuthenticate:
                    return H2StaticTable.ProxyAuthenticate;
                case KnownHeaderType.RetryAfter:
                    return H2StaticTable.RetryAfter;
                case KnownHeaderType.Server:
                    return H2StaticTable.Server;
                case KnownHeaderType.SetCookie:
                    return H2StaticTable.SetCookie;
                case KnownHeaderType.Vary:
                    return H2StaticTable.Vary;
                case KnownHeaderType.WWWAuthenticate:
                    return H2StaticTable.WwwAuthenticate;
                case KnownHeaderType.AccessControlAllowOrigin:
                    return H2StaticTable.AccessControlAllowOrigin;
                case KnownHeaderType.ContentLength:
                    return H2StaticTable.ContentLength;
                default:
                    return -1;
            }
        }
    }
}
