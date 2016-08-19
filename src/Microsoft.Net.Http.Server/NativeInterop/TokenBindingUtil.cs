// Copyright (c) .NET Foundation.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Runtime.InteropServices;
using static Microsoft.Net.Http.Server.HttpApi;
using static Microsoft.Net.Http.Server.UnsafeNclNativeMethods.TokenBinding;

namespace Microsoft.Net.Http.Server
{
    /// <summary>
    /// Contains helpers for dealing with TLS token binding.
    /// </summary>
    // TODO: https://github.com/aspnet/WebListener/issues/231
    internal unsafe static class TokenBindingUtil
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
        public static byte[] GetProvidedTokenIdFromBindingInfo(HTTP_REQUEST_TOKEN_BINDING_INFO* pTokenBindingInfo, out byte[] referredId)
        {
            byte[] providedId = null;
            referredId = null;

            HeapAllocHandle handle = null;
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
}
