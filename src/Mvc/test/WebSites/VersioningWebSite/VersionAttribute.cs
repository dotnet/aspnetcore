// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace VersioningWebSite
{
    public class VersionAttribute : Attribute, IActionConstraintFactory
    {
        private int? _maxVersion;
        private int? _minVersion;
        private int? _order;

        public int MinVersion
        {
            get { return _minVersion ?? -1; }
            set { _minVersion = value; }
        }

        public int MaxVersion
        {
            get { return _maxVersion ?? -1; }
            set { _maxVersion = value; }
        }

        public int Order
        {
            get { return _order ?? -1; }
            set { _order = value; }
        }

        public bool IsReusable => true;

        IActionConstraint IActionConstraintFactory.CreateInstance(IServiceProvider services)
        {
            return new VersionRangeValidator(_minVersion, _maxVersion) { Order = _order ?? 0 };
        }
    }
}