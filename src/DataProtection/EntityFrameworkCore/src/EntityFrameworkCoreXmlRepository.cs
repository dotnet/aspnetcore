// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

/// <summary>
/// An <see cref="IXmlRepository"/> backed by an EntityFrameworkCore datastore.
/// </summary>
public class EntityFrameworkCoreXmlRepository<TContext> : IXmlRepository
    where TContext : DbContext, IDataProtectionKeyContext
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of the <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    // DataProtectionKey.Id is not used anywhere. Add DynamicDependency to prevent it from being trimmed.
    // Note that in the future EF may annotate itself to include properties automatically, and the annotation here could be removed.
    // Fixes https://github.com/dotnet/aspnetcore/issues/43187
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(DataProtectionKey))]
    public EntityFrameworkCoreXmlRepository(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<EntityFrameworkCoreXmlRepository<TContext>>();
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    public virtual IReadOnlyCollection<XElement> GetAllElements()
    {
        // forces complete enumeration
        return GetAllElementsCore().ToList().AsReadOnly();

        IEnumerable<XElement> GetAllElementsCore()
        {
            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                foreach (var key in context.DataProtectionKeys.AsNoTracking())
                {
                    _logger.ReadingXmlFromKey(key.FriendlyName!, key.Xml);

                    if (!string.IsNullOrEmpty(key.Xml))
                    {
                        yield return XElement.Parse(key.Xml);
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName)
    {
        using (var scope = _services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            var newKey = new DataProtectionKey()
            {
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting)
            };

            context.DataProtectionKeys.Add(newKey);
            _logger.LogSavingKeyToDbContext(friendlyName, typeof(TContext).Name);
            context.SaveChanges();
        }
    }
}
