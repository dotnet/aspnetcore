// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.DataHandler.Serializer
{
    public interface IDataSerializer<TModel>
    {
        byte[] Serialize(TModel model);
        TModel Deserialize(byte[] data);
    }
}
