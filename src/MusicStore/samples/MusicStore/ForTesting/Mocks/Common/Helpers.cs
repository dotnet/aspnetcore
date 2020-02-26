// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace MusicStore.Mocks.Common
{
    internal class Helpers
    {
        internal static void ThrowIfConditionFailed(Func<bool> condition, string errorMessage)
        {
            if (!condition())
            {
                throw new Exception(errorMessage);
            }
        }
    }
} 
