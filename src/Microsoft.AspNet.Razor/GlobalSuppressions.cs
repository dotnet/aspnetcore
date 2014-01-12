// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

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

[assembly: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "br", Scope = "resource", Target = "System.Web.Razor.Resources.RazorResources.resources", Justification = "Resource is referencing html tag")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Web.Razor.Tokenizer.Symbols", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Web.Razor.Tokenizer", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Web.Razor.Text", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Web.Razor.Parser", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Web.Razor.Editor", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Web.Razor", Justification = "These namespaces are design to group classes by function. They will be reviewed to ensure they remain relevant.")]
[assembly: SuppressMessage("Microsoft.Web.FxCop", "MW1000:UnusedResourceUsageRule", Justification = "There are numerous unused resources due to VB being disabled. This rule will be re-run after VB is restored")]
