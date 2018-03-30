// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor MVC7000_ApiActionsMustBeAttributeRouted =
            new DiagnosticDescriptor(
                "MVC7000",
                "Actions on types annotated with ApiControllerAttribute must be attribute routed.",
                "Actions on types annotated with ApiControllerAttribute must be attribute routed.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC7001_ApiActionsHaveBadModelStateFilter =
            new DiagnosticDescriptor(
                "MVC7001",
                "Actions on types annotated with ApiControllerAttribute do not require explicit ModelState validity check.",
                "Actions on types annotated with ApiControllerAttribute do not require explicit ModelState validity check.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC7002_ApiActionsShouldReturnActionResultOf =
            new DiagnosticDescriptor(
                "MVC7002",
                "Actions on types annotated with ApiControllerAttribute should return ActionResult<T>.",
                "Actions on types annotated with ApiControllerAttribute should return ActionResult<T>.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC7003_ActionsMustNotBeAsyncVoid =
            new DiagnosticDescriptor(
                "MVC7003",
                "Controller actions must not have async void signature.",
                "Controller actions must not have async void signature.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
    }
}
