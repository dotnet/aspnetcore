// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    internal class OptionsWrapperMonitor<T> : IOptionsMonitor<T>
    {
        public OptionsWrapperMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            return null;
        }

        public T Get(string name) => CurrentValue;

        public T CurrentValue { get; }
    }
}