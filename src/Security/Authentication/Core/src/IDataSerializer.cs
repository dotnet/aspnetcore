// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Authentication
{
    public interface IDataSerializer<TModel>
    {
        byte[] Serialize(TModel model);

        [return: MaybeNull]
        TModel Deserialize(byte[] data);
    }
}
