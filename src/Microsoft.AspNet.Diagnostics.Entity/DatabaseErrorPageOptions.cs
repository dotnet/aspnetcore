// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class DatabaseErrorPageOptions
    {
        private PathString _migrationsEndPointPath = MigrationsEndPointOptions.DefaultPath;
        private bool _defaultVisibility;
        private bool? _showExceptionDetails;
        private bool? _listMigrations;
        private bool? _enableMigrationCommands;

        public static DatabaseErrorPageOptions ShowAll
        {
            get
            {
                // We don't use a static instance because it's mutable.
                return new DatabaseErrorPageOptions()
                {
                    ShowExceptionDetails = true,
                    ListMigrations = true,
                    EnableMigrationCommands = true
                };
            }
        }

        public virtual PathString MigrationsEndPointPath
        {
            get { return _migrationsEndPointPath; }
            set
            {
                Check.NotNull(value, "value");
                _migrationsEndPointPath = value;
            }
        }

        public virtual bool ShowExceptionDetails
        {
            get { return _showExceptionDetails ?? _defaultVisibility; }
            set { _showExceptionDetails = value; }
        }

        public virtual bool ListMigrations
        {
            get { return _listMigrations ?? _defaultVisibility; }
            set { _listMigrations = value; }
        }

        public virtual bool EnableMigrationCommands
        {
            get { return _enableMigrationCommands ?? _defaultVisibility; }
            set { _enableMigrationCommands = value; }
        }

        public virtual void SetDefaultVisibility(bool isVisible)
        {
            _defaultVisibility = isVisible;
        }
    }
}
