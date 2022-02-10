// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace VersioningWebSite;

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
