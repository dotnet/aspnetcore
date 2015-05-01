// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Error List, point to "Suppress Message(s)", and click
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "br", Scope = "resource", Target = "Microsoft.AspNet.Razor.Resources.RazorResources.resources", Justification = "Resource is referencing html tag")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.AspNet.Razor.Tokenizer.Symbols", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.AspNet.Razor.Tokenizer", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.AspNet.Razor.Text", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.AspNet.Razor.Parser", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.AspNet.Razor.Editor", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.AspNet.Razor", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
