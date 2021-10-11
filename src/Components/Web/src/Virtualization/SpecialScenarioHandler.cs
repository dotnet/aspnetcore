// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization
{
    internal class SpecialScenarioHandler
    {
        private readonly ContainerKind _containerKind;

        public SpecialScenarioHandler(ContainerKind containerKind)
        {
            _containerKind = containerKind;
        }

        public string GetAdditionalCssStyleProperties() => _containerKind switch
        {
            ContainerKind.HTMLTable => "display:table-row;",
            _ => throw new NotImplementedException()
        };
    }
}
