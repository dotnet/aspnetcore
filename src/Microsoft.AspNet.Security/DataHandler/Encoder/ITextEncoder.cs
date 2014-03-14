// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.DataHandler.Encoder
{
    public interface ITextEncoder
    {
        string Encode(byte[] data);
        byte[] Decode(string text);
    }
}
