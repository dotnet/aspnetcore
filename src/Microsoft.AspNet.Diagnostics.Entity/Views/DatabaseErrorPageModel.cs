// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Diagnostics.Entity.Views
{
    public class DatabaseErrorPageModel
    {
        private readonly Type _contextType;
        private readonly Exception _exception;
        private readonly bool _databaseExists;
        private readonly bool _pendingModelChanges;
        private readonly IEnumerable<string> _pendingMigrations;
        private readonly DatabaseErrorPageOptions _options;

        public DatabaseErrorPageModel(
            [NotNull] Type contextType,
            [NotNull] Exception exception,
            bool databaseExists,
            bool pendingModelChanges,
            [NotNull] IEnumerable<string> pendingMigrations,
            [NotNull] DatabaseErrorPageOptions options)
        {
            Check.NotNull(contextType, "contextType");
            Check.NotNull(exception, "exception");
            Check.NotNull(pendingMigrations, "pendingMigrations");
            Check.NotNull(options, "options");

            _contextType = contextType;
            _exception = exception;
            _databaseExists = databaseExists;
            _pendingModelChanges = pendingModelChanges;
            _pendingMigrations = pendingMigrations;
            _options = options;
        }

        public virtual Type ContextType
        {
            get { return _contextType; }
        }

        public virtual Exception Exception
        {
            get { return _exception; }
        }

        public virtual bool DatabaseExists
        {
            get { return _databaseExists; }
        }

        public virtual bool PendingModelChanges
        {
            get { return _pendingModelChanges; }
        }

        public virtual IEnumerable<string> PendingMigrations
        {
            get { return _pendingMigrations; }
        }

        public virtual DatabaseErrorPageOptions Options
        {
            get { return _options; }
        }
    }
}
