// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    internal interface ICommand
    {
        void Execute(SecretsStore store, ILogger logger);
    }
}