// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
{
    /// <summary>
    /// An <see cref="IXmlRepository"/> backed by an EntityFrameworkCore datastore.
    /// </summary>
    public class EntityFrameworkCoreXmlRepository<TContext> : IXmlRepository
        where TContext : DbContext, IDataProtectionKeyContext
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Func<TContext> _contextFactory;

        private ILogger<EntityFrameworkCoreXmlRepository<TContext>> _logger => _loggerFactory?.CreateLogger<EntityFrameworkCoreXmlRepository<TContext>>();

        private TContext _context => _contextFactory?.Invoke();

        /// <summary>
        /// Creates a new instance of the <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
        /// </summary>
        /// <param name="contextFactory">The factory method that creates a context to store instances of <see cref="DataProtectionKey"/></param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public EntityFrameworkCoreXmlRepository(Func<TContext> contextFactory, ILoggerFactory loggerFactory = null)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public virtual IReadOnlyCollection<XElement> GetAllElements()
            => _context?.Set<DataProtectionKey>()?.AsNoTracking().Select(key => TryParseKeyXml(key.Xml)).ToList().AsReadOnly();

        /// <inheritdoc />
        public void StoreElement(XElement element, string friendlyName)
        {
            var newKey = new DataProtectionKey()
            {
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting)
            };
            var context = _context;
            context?.Set<DataProtectionKey>()?.Add(newKey);
            _logger?.LogSavingKeyToDbContext(friendlyName, typeof(TContext).Name);
            context?.SaveChanges();
        }

        private XElement TryParseKeyXml(string xml)
        {
            try
            {
                return XElement.Parse(xml);
            }
            catch (Exception e)
            {
                _logger?.LogExceptionWhileParsingKeyXml(xml, e);
                return null;
            }
        }
    }
}
