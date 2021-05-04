// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Wasm.Performance.TestApp.Pages;

namespace Wasm.Performance.ConsoleHost.Scenarios
{
    internal class GridScenario : ComponentRenderingScenarioBase
    {
        readonly CommandOption _gridTypeOption = new CommandOption("--gridtype", CommandOptionType.SingleValue);

        public GridScenario() : base("grid")
        {
            Options.Add(_gridTypeOption);
        }

        protected override async Task ExecuteAsync(ConsoleHostRenderer renderer, int numCycles)
        {
            var gridType = _gridTypeOption.HasValue()
                ? (GridRendering.RenderMode)Enum.Parse(typeof(GridRendering.RenderMode), _gridTypeOption.Value(), true)
                : GridRendering.RenderMode.FastGrid;

            for (var i = 0; i < numCycles; i++)
            {
                var hostPage = new GridRendering { SelectedRenderMode = gridType };
                hostPage.Show();

                var componentId = renderer.AssignRootComponentId(hostPage);
                await renderer.RenderRootComponentAsync(componentId);

                hostPage.ChangePage();
                await renderer.RenderRootComponentAsync(componentId);
            }
        }
    }
}
