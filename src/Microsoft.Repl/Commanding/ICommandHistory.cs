// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Repl.Commanding
{
    public interface ICommandHistory
    {
        string GetPreviousCommand();

        string GetNextCommand();

        void AddCommand(string command);

        IDisposable SuspendHistory();
    }
}
