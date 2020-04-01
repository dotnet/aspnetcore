// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class TestRegistryKey : IDisposable
    {
        private readonly RegistryKey _baseHive;
        private readonly RegistryKey _subKey;
        private readonly string _keyName;

        public TestRegistryKey(RegistryKey baseHive, string keyName, string valueName, object value)
        {
            _baseHive = baseHive;
            _keyName = keyName;
            _subKey = baseHive.CreateSubKey(keyName);
            _subKey.SetValue(valueName, value);
        }

        public void Dispose()
        {
            _baseHive.DeleteSubKeyTree(_keyName, throwOnMissingSubKey: true);
        }
    }
}
