// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace HtmlGenerationWebSite
{
    public interface ISignalTokenProviderService<TKey>
    {
        IChangeToken GetToken(object key);

        void SignalToken(object key);
    }
}