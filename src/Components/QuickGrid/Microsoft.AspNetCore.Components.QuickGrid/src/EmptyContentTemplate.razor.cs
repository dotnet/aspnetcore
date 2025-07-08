using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;

namespace Microsoft.AspNetCore.Components.QuickGrid;

public partial class EmptyContentTemplate<TGridItem>
{
    [CascadingParameter] internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    [Parameter] public RenderFragment ChildContent { get; set; }
}
