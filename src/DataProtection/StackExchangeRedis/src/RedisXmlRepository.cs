// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.DataProtection.StackExchangeRedis;

/// <summary>
/// An XML repository backed by a Redis list entry.
/// </summary>
public class RedisXmlRepository : IXmlRepository
{
    private readonly Func<IDatabase> _databaseFactory;
    private readonly RedisKey _key;

    /// <summary>
    /// Creates a <see cref="RedisXmlRepository"/> with keys stored at the given directory.
    /// </summary>
    /// <param name="databaseFactory">The delegate used to create <see cref="IDatabase"/> instances.</param>
    /// <param name="key">The <see cref="RedisKey"/> used to store key list.</param>
    public RedisXmlRepository(Func<IDatabase> databaseFactory, RedisKey key)
    {
        _databaseFactory = databaseFactory;
        _key = key;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        return GetAllElementsCore().ToList().AsReadOnly();
    }

    private IEnumerable<XElement> GetAllElementsCore()
    {
        // Note: Inability to read any value is considered a fatal error (since the file may contain
        // revocation information), and we'll fail the entire operation rather than return a partial
        // set of elements. If a value contains well-formed XML but its contents are meaningless, we
        // won't fail that operation here. The caller is responsible for failing as appropriate given
        // that scenario.
        var database = _databaseFactory();
        foreach (var value in database.ListRange(_key))
        {
            yield return XElement.Parse((string)value!);
        }
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName)
    {
        var database = _databaseFactory();
        database.ListRightPush(_key, element.ToString(SaveOptions.DisableFormatting));
    }
}
