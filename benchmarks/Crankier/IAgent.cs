// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public interface IAgent
    {
        Task PongAsync(int id, int value);
        Task LogAsync(int id, string text);
        Task StatusAsync(int id, StatusInformation statusInformation);
    }
}
