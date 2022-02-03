// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal partial class DefaultActionDescriptorCollectionProvider : ActionDescriptorCollectionProvider
{
    private readonly IActionDescriptorProvider[] _actionDescriptorProviders;
    private readonly IActionDescriptorChangeProvider[] _actionDescriptorChangeProviders;
    private readonly ILogger _logger;

    // The lock is used to protect WRITES to the following (do not need to protect reads once initialized).
    private readonly object _lock;
    private ActionDescriptorCollection? _collection;
    private IChangeToken? _changeToken;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _version;

    public DefaultActionDescriptorCollectionProvider(
        IEnumerable<IActionDescriptorProvider> actionDescriptorProviders,
        IEnumerable<IActionDescriptorChangeProvider> actionDescriptorChangeProviders,
        ILogger<DefaultActionDescriptorCollectionProvider> logger)
    {
        _actionDescriptorProviders = actionDescriptorProviders
            .OrderBy(p => p.Order)
            .ToArray();

        _actionDescriptorChangeProviders = actionDescriptorChangeProviders.ToArray();

        _lock = new object();

        _logger = logger;

        // IMPORTANT: this needs to be the last thing we do in the constructor. Change notifications can happen immediately!
        ChangeToken.OnChange(
            GetCompositeChangeToken,
            UpdateCollection);
    }

    /// <summary>
    /// Returns a cached collection of <see cref="ActionDescriptor" />.
    /// </summary>
    public override ActionDescriptorCollection ActionDescriptors
    {
        get
        {
            Initialize();
            Debug.Assert(_collection != null);
            Debug.Assert(_changeToken != null);

            return _collection;
        }
    }

    /// <summary>
    /// Gets an <see cref="IChangeToken"/> that will be signaled after the <see cref="ActionDescriptors"/>
    /// collection has changed.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public override IChangeToken GetChangeToken()
    {
        Initialize();
        Debug.Assert(_collection != null);
        Debug.Assert(_changeToken != null);

        return _changeToken;
    }

    private IChangeToken GetCompositeChangeToken()
    {
        if (_actionDescriptorChangeProviders.Length == 1)
        {
            return _actionDescriptorChangeProviders[0].GetChangeToken();
        }

        var changeTokens = new IChangeToken[_actionDescriptorChangeProviders.Length];
        for (var i = 0; i < _actionDescriptorChangeProviders.Length; i++)
        {
            changeTokens[i] = _actionDescriptorChangeProviders[i].GetChangeToken();
        }

        return new CompositeChangeToken(changeTokens);
    }

    private void Initialize()
    {
        // Using double-checked locking on initialization because we fire change token callbacks
        // when the collection changes. We don't want to do that repeatedly for redundant changes.
        //
        // The main call path of this code on the first call is async initialization from Endpoint Routing
        // which is done in a non-blocking way so in practice no caller will ever block here.
        if (_collection == null)
        {
            lock (_lock)
            {
                if (_collection == null)
                {
                    UpdateCollection();
                }
            }
        }
    }

    private void UpdateCollection()
    {
        // Using the lock to initialize writes means that we serialize changes. This eliminates
        // the potential for changes to be processed out of order - the risk is that newer data
        // could be overwritten by older data.
        lock (_lock)
        {
            var context = new ActionDescriptorProviderContext();

            for (var i = 0; i < _actionDescriptorProviders.Length; i++)
            {
                _actionDescriptorProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _actionDescriptorProviders.Length - 1; i >= 0; i--)
            {
                _actionDescriptorProviders[i].OnProvidersExecuted(context);
            }

            if (context.Results.Count == 0)
            {
                // Emit a log message if after all providers still no action
                // descriptors detected in the context.
                Log.NoActionDescriptors(_logger);
            }

            // The sequence for an update is important because we don't want anyone to obtain
            // the new change token but the old action descriptor collection.
            // 1. Obtain the old cancellation token source (don't trigger it yet)
            // 2. Set the new action descriptor collection
            // 3. Set the new change token
            // 4. Trigger the old cancellation token source
            //
            // Consumers who poll will observe a new action descriptor collection at step 2 - they will see
            // the new collection and ignore the change token.
            //
            // Consumers who listen to the change token will re-query at step 4 - they will see the new collection
            // and new change token.
            //
            // Anyone who acquires the collection and change token between steps 2 and 3 will be notified of
            // a no-op change at step 4.

            // Step 1.
            var oldCancellationTokenSource = _cancellationTokenSource;

            // Step 2.
            _collection = new ActionDescriptorCollection(
                new ReadOnlyCollection<ActionDescriptor>(context.Results),
                _version++);

            // Step 3.
            _cancellationTokenSource = new CancellationTokenSource();
            _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);

            // Step 4 - might be null if it's the first time.
            oldCancellationTokenSource?.Cancel();
        }
    }

    public static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            EventName = "NoActionDescriptors",
            Level = LogLevel.Information,
            Message = "No action descriptors found. This may indicate an incorrectly configured application or missing application parts. To learn more, visit https://aka.ms/aspnet/mvc/app-parts")]
        public static partial void NoActionDescriptors(ILogger logger);
    }
}
