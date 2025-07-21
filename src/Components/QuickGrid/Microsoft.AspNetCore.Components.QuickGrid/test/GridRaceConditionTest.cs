// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.QuickGrid.Test;

public class GridRaceConditionTest
{
    private TestRenderer _renderer = new();
    private TaskCompletionSource _tcs = new();

    public GridRaceConditionTest()
    {
        var testJsRuntime = new TestJsRuntime(_tcs);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(testJsRuntime)
            .BuildServiceProvider();
        _renderer = new(serviceProvider);
    }

    [Fact]
    public async Task CanCorrectlyDisposeAsync()
    {
        var testComponent = new TestComponent();

        var componentId = _renderer.AssignRootComponentId(testComponent);
        _renderer.RenderRootComponent(componentId);
        await Task.Delay(10);
        _renderer.RenderRootComponent(componentId);
        _tcs.SetResult();
    }
}

internal class TestComponent : ComponentBase
{
    private bool _firstRender = true;
    private PaginationState _pagination = new() { ItemsPerPage = 2 };

    internal class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private IQueryable<Person> _people = new List<Person>
    {
        new Person { Id = 1, Name = "John Doe", Age = 30 },
        new Person { Id = 2, Name = "Jane Smith", Age = 25 },
        new Person { Id = 3, Name = "Alice Johnson", Age = 22 }
    }.AsQueryable();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_firstRender)
        {
            //Render the QuickGrid
            builder.OpenComponent<QuickGrid<Person>>(0);
            builder.AddAttribute(1, "Items", _people);
            builder.AddAttribute(2, "Pagination", _pagination);
            builder.AddAttribute(3, nameof(QuickGrid<Person>.ChildContent),
                (RenderFragment)(builder => BuildColumnsRenderFragment(builder)));
            builder.CloseComponent();

            builder.OpenComponent<Paginator>(4);
            builder.AddAttribute(5, "State", _pagination);
            builder.CloseComponent();
            _firstRender = false;
        }
    }

    protected void BuildColumnsRenderFragment(RenderTreeBuilder builder)
    {
        //Render the PropertyColumn for Id
        builder.OpenComponent<PropertyColumn<Person, int>>(0);
        builder.AddAttribute(1, nameof(PropertyColumn<Person, int>.Property),
            (System.Linq.Expressions.Expression<Func<Person, int>>)(p => p.Id));
        builder.AddAttribute(2, nameof(PropertyColumn<Person, int>.Sortable), true);
        builder.CloseComponent();

        //Render the PropertyColumn for Name
        builder.OpenComponent<PropertyColumn<Person, string>>(3);
        builder.AddAttribute(4, nameof(PropertyColumn<Person, string>.Property),
            (System.Linq.Expressions.Expression<Func<Person, string>>)(p => p.Name));
        builder.AddAttribute(5, nameof(PropertyColumn<Person, string>.Sortable), true);
        builder.CloseComponent();

        //Render the PropertyColumn for Age
        builder.OpenComponent<PropertyColumn<Person, int>>(6);
        builder.AddAttribute(7, nameof(PropertyColumn<Person, int>.Property),
            (System.Linq.Expressions.Expression<Func<Person, int>>)(p => p.Age));
        builder.AddAttribute(8, nameof(PropertyColumn<Person, int>.Sortable), true);
        builder.CloseComponent();
    }

    public void TriggerRender()
    {
        InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Task.Delay(1);
            StateHasChanged();
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}

internal class TestJsRuntime(TaskCompletionSource tcs) : IJSRuntime
{
    private readonly TaskCompletionSource _tcs = tcs;

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args = null)
    {
        if (identifier == "import" && args != null && args.Length > 0 && args[0] is string modulePath)
        {
            if (modulePath == "./_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js")
            {
                await _tcs.Task;
                return default!;
            }
        }
        throw new Exception("JS import was not correctly processed while disposing of the component.");
    }

    public ValueTask<IJSObjectReference> InvokeAsync(string identifier, params object[] args)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
    {
        throw new NotImplementedException();
    }
}
