// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class DatabaseErrorPageOptions
    {
        private bool _defaultVisibility;
        private bool? _showExceptionDetails;
        private bool? _listMigrations;

        public static DatabaseErrorPageOptions ShowAll
        {
            get
            {
                // We don't use a static instance because it's mutable.
                return new DatabaseErrorPageOptions()
                {
                    ShowExceptionDetails = true,
                    ListMigrations = true,
                };
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

        public virtual void SetDefaultVisibility(bool isVisible)
        {
            _defaultVisibility = isVisible;
        }
    }
}
