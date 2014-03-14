// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security
{
    public interface ISecureDataFormat<TData>
    {
        string Protect(TData data);
        TData Unprotect(string protectedText);
    }
}
