// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Microsoft.AspNet.Mvc.Rendering.Expressions
{
    public static class TryGetValueProvider
    {
        private static readonly Dictionary<Type, TryGetValueDelegate> _tryGetValueDelegateCache =
            new Dictionary<Type, TryGetValueDelegate>();
        private static readonly ReaderWriterLockSlim _tryGetValueDelegateCacheLock = new ReaderWriterLockSlim();

        // Information about private static method declared below.
        private static readonly MethodInfo _strongTryGetValueImplInfo =
            typeof(TryGetValueProvider).GetTypeInfo().GetDeclaredMethod("StrongTryGetValueImpl");

        public static TryGetValueDelegate CreateInstance([NotNull] Type targetType)
        {
            TryGetValueDelegate result;

            // Cache delegates since properties of model types are re-evaluated numerous times.
            _tryGetValueDelegateCacheLock.EnterReadLock();
            try
            {
                if (_tryGetValueDelegateCache.TryGetValue(targetType, out result))
                {
                    return result;
                }
            }
            finally
            {
                _tryGetValueDelegateCacheLock.ExitReadLock();
            }

            var dictionaryType = targetType.ExtractGenericInterface(typeof(IDictionary<,>));

            // Just wrap a call to the underlying IDictionary<TKey, TValue>.TryGetValue() where string can be cast to TKey.
            if (dictionaryType != null)
            {
                var typeArguments = dictionaryType.GetGenericArguments();
                var keyType = typeArguments[0];
                var returnType = typeArguments[1];

                if (keyType.IsAssignableFrom(typeof(string)))
                {
                    var implementationMethod = _strongTryGetValueImplInfo.MakeGenericMethod(keyType, returnType);
                    result = (TryGetValueDelegate)implementationMethod.CreateDelegate(typeof(TryGetValueDelegate));
                }
            }

            // Wrap a call to the underlying IDictionary.Item().
            if (result == null && typeof(IDictionary).IsAssignableFrom(targetType))
            {
                result = TryGetValueFromNonGenericDictionary;
            }

            _tryGetValueDelegateCacheLock.EnterWriteLock();
            try
            {
                _tryGetValueDelegateCache[targetType] = result;
            }
            finally
            {
                _tryGetValueDelegateCacheLock.ExitWriteLock();
            }

            return result;
        }

        private static bool StrongTryGetValueImpl<TKey, TValue>(object dictionary, string key, out object value)
        {
            var strongDict = (IDictionary<TKey, TValue>)dictionary;

            TValue strongValue;
            var success = strongDict.TryGetValue((TKey)(object)key, out strongValue);
            value = strongValue;
            return success;
        }

        private static bool TryGetValueFromNonGenericDictionary(object dictionary, string key, out object value)
        {
            var weakDict = (IDictionary)dictionary;

            var success = weakDict.Contains(key);
            value = success ? weakDict[key] : null;
            return success;
        }
    }
}