// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Filter that controls whether component state should be persisted during prerendering.
/// </summary>
public class PersistOnPrerenderingFilter(bool persist = true) : PersistReasonFilter<PersistOnPrerendering>(persist);

/// <summary>
/// Filter that controls whether component state should be persisted during enhanced navigation.
/// </summary>
public class PersistOnEnhancedNavigationFilter(bool persist = true) : PersistReasonFilter<PersistOnEnhancedNavigation>(persist);

/// <summary>
/// Filter that controls whether component state should be persisted when a circuit is paused.
/// </summary>
public class PersistOnCircuitPauseFilter(bool persist = true) : PersistReasonFilter<PersistOnCircuitPause>(persist);