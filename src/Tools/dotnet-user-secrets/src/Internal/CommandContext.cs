// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public class CommandContext
    {
        public CommandContext(
            SecretsStore store,
            IReporter reporter,
            IConsole console)
        {
            SecretStore = store;
            Reporter = reporter;
            Console = console;
        }

        public IConsole Console { get; }
        public IReporter Reporter { get; }
        public SecretsStore SecretStore { get; }
    }
}