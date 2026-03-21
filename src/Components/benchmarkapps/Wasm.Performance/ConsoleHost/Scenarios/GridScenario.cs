// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Wasm.Performance.TestApp.Pages;

namespace Wasm.Performance.ConsoleHost.Scenarios;

internal sealed class GridScenario : ComponentRenderingScenarioBase
{
    readonly CommandOption _gridTypeOption = new CommandOption("--gridtype", CommandOptionType.SingleValue);

    public GridScenario() : base("grid")
    {
        Options.Add(_gridTypeOption);
    }

    protected override async Task ExecuteAsync(ConsoleHostRenderer renderer, int numCycles)
    {
        var gridType = _gridTypeOption.HasValue()
            ? (GridRendering.GridRenderMode)Enum.Parse(typeof(GridRendering.GridRenderMode), _gridTypeOption.Value(), true)
            : GridRendering.GridRenderMode.FastGrid;

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
