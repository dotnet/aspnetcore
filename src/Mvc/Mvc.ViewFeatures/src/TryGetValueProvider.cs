// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Static class that provides caching for TryGetValue. This class cannot be inherited.
/// </summary>
public static class TryGetValueProvider
{
    private static readonly Dictionary<Type, TryGetValueDelegate> _tryGetValueDelegateCache =
        new Dictionary<Type, TryGetValueDelegate>();
    private static readonly ReaderWriterLockSlim _tryGetValueDelegateCacheLock = new ReaderWriterLockSlim();

    // Information about private static method declared below.
    private static readonly MethodInfo _strongTryGetValueImplInfo =
        typeof(TryGetValueProvider).GetTypeInfo().GetDeclaredMethod(nameof(StrongTryGetValueImpl));

    /// <summary>
    /// Returns a <see cref="TryGetValueDelegate"/> for the specified <see cref="IDictionary{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="targetType">The target type that is expected to be a <see cref="IDictionary{TKey, TValue}"/>.</param>
    /// <returns>The <see cref="TryGetValueDelegate"/>.</returns>
    public static TryGetValueDelegate CreateInstance(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

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
