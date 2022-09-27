// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using static Microsoft.AspNetCore.HttpSys.Internal.HttpApiTypes;
using static Microsoft.AspNetCore.HttpSys.Internal.UnsafeNclNativeMethods.TokenBinding;

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Contains helpers for dealing with TLS token binding.
/// </summary>
// TODO: https://github.com/aspnet/HttpSysServer/issues/231
internal static unsafe class TokenBindingUtil
{
    private static byte[] ExtractIdentifierBlob(TOKENBINDING_RESULT_DATA* pTokenBindingResultData)
    {
        // Per http://tools.ietf.org/html/draft-ietf-tokbind-protocol-00, Sec. 4,
        // the identifier is a tuple which contains (token binding type, hash algorithm
        // signature algorithm, key data). We'll strip off the token binding type and
        // return the remainder (starting with the hash algorithm) as an opaque blob.
        byte[] retVal = new byte[checked(pTokenBindingResultData->identifierSize - 1)];
        Marshal.Copy((IntPtr)(&pTokenBindingResultData->identifierData->hashAlgorithm), retVal, 0, retVal.Length);
        return retVal;
    }

    /// <summary>
    /// Returns the 'provided' token binding identifier, optionally also returning the
    /// 'referred' token binding identifier. Returns null on failure.
    /// </summary>
    public static byte[]? GetProvidedTokenIdFromBindingInfo(HTTP_REQUEST_TOKEN_BINDING_INFO* pTokenBindingInfo, out byte[]? referredId)
    {
        byte[]? providedId = null;
        referredId = null;

        HeapAllocHandle? handle = null;
        int status = UnsafeNclNativeMethods.TokenBindingVerifyMessage(
            pTokenBindingInfo->TokenBinding,
            pTokenBindingInfo->TokenBindingSize,
            pTokenBindingInfo->KeyType,
            pTokenBindingInfo->TlsUnique,
            pTokenBindingInfo->TlsUniqueSize,
            out handle);

        // No match found or there was an error?
        if (status != 0 || handle == null || handle.IsInvalid)
        {
            return null;
        }

        using (handle)
        {
            // Find the first 'provided' and 'referred' types.
            TOKENBINDING_RESULT_LIST* pResultList = (TOKENBINDING_RESULT_LIST*)handle.DangerousGetHandle();
            for (int i = 0; i < pResultList->resultCount; i++)
            {
                TOKENBINDING_RESULT_DATA* pThisResultData = &pResultList->resultData[i];
                if (pThisResultData->identifierData->bindingType == TOKENBINDING_TYPE.TOKENBINDING_TYPE_PROVIDED)
                {
                    if (providedId != null)
                    {
                        return null; // It is invalid to have more than one 'provided' identifier.
                    }
                    providedId = ExtractIdentifierBlob(pThisResultData);
                }
                else if (pThisResultData->identifierData->bindingType == TOKENBINDING_TYPE.TOKENBINDING_TYPE_REFERRED)
                {
                    if (referredId != null)
                    {
                        return null; // It is invalid to have more than one 'referred' identifier.
                    }
                    referredId = ExtractIdentifierBlob(pThisResultData);
                }
            }
        }

        return providedId;
    }
}
