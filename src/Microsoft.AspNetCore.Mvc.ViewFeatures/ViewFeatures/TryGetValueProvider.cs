// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public static class TryGetValueProvider
    {
        private static readonly Dictionary<Type, TryGetValueDelegate> _tryGetValueDelegateCache =
            new Dictionary<Type, TryGetValueDelegate>();
        private static readonly ReaderWriterLockSlim _tryGetValueDelegateCacheLock = new ReaderWriterLockSlim();

        // Information about private static method declared below.
        private static readonly MethodInfo _strongTryGetValueImplInfo =
            typeof(TryGetValueProvider).GetTypeInfo().GetDeclaredMethod(nameof(StrongTryGetValueImpl));

        public static TryGetValueDelegate CreateInstance(Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

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

            var dictionaryType = ClosedGenericMatcher.ExtractGenericInterface(targetType, typeof(IDictionary<,>));

            // Just wrap a call to the underlying IDictionary<TKey, TValue>.TryGetValue() where string can be cast to
            // TKey.
            if (dictionaryType != null)
            {
                var typeArguments = dictionaryType.GenericTypeArguments;
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