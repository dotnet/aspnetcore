// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "responseheaderhash.h"
#include "exceptions.h"

HEADER_RECORD RESPONSE_HEADER_HASH::sm_rgHeaders[] =
{
    { "Cache-Control",       HttpHeaderCacheControl       },
    { "Connection",          HttpHeaderConnection         },
    { "Date",                HttpHeaderDate               },
    { "Keep-Alive",          HttpHeaderKeepAlive          },
    { "Pragma",              HttpHeaderPragma             },
    { "Trailer",             HttpHeaderTrailer            },
    { "Transfer-Encoding",   HttpHeaderTransferEncoding   },
    { "Upgrade",             HttpHeaderUpgrade            },
    { "Via",                 HttpHeaderVia                },
    { "Warning",             HttpHeaderWarning            },
    { "Allow",               HttpHeaderAllow              },
    { "Content-Length",      HttpHeaderContentLength      },
    { "Content-Type",        HttpHeaderContentType        },
    { "Content-Encoding",    HttpHeaderContentEncoding    },
    { "Content-Language",    HttpHeaderContentLanguage    },
    { "Content-Location",    HttpHeaderContentLocation    },
    { "Content-MD5",         HttpHeaderContentMd5         },
    { "Content-Range",       HttpHeaderContentRange       },
    { "Expires",             HttpHeaderExpires            },
    { "Last-Modified",       HttpHeaderLastModified       },
    { "Accept-Ranges",       HttpHeaderAcceptRanges       },
    { "Age",                 HttpHeaderAge                },
    { "ETag",                HttpHeaderEtag               },
    { "Location",            HttpHeaderLocation           },
    { "Proxy-Authenticate",  HttpHeaderProxyAuthenticate  },
    { "Retry-After",         HttpHeaderRetryAfter         },
    { "Server",              HttpHeaderServer             },
    // Set it to something which cannot be a header name, in effect
    // making Server an unknown header. w:w is used to avoid collision with Keep-Alive.
    { "w:w\r\n",             HttpHeaderServer             },
    // Set it to something which cannot be a header name, in effect
    // making Set-Cookie an unknown header
    { "y:y\r\n",             HttpHeaderSetCookie          },
    { "Vary",                HttpHeaderVary               },
    // Set it to something which cannot be a header name, in effect
    // making WWW-Authenticate an unknown header
    { "z:z\r\n",             HttpHeaderWwwAuthenticate    }

};

HRESULT
RESPONSE_HEADER_HASH::Initialize(
    VOID
)
/*++

Routine Description:

    Initialize global header hash table

Arguments:

    None

Return Value:

    HRESULT

--*/
{
    //
    // 31 response headers.
    // Make sure to update the number of buckets it new headers
    // are added. Test it to avoid collisions.
    //
    C_ASSERT(_countof(sm_rgHeaders) == 31);

    //
    // 79 buckets will have less collisions for the 31 response headers.
    // Known collisions are "Age" colliding with "Expire" and "Location"
    // colliding with both "Expire" and "Age".
    //
    RETURN_IF_FAILED(HASH_TABLE::Initialize(79));

    for ( DWORD Index = 0; Index < _countof(sm_rgHeaders); ++Index )
    {
        RETURN_IF_FAILED(InsertRecord(&sm_rgHeaders[Index]));
    }

    return S_OK;
}

