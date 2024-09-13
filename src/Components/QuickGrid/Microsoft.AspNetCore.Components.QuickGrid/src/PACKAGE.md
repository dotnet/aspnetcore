## About

`Microsoft.AspNetCore.Components.QuickGrid` provides a simple and convenient data grid component for common grid rendering scenarios.

## Key Features

* Pagination
* Filtering
* Sorting
* Virtualization
* Support for in-memory `IQueryable`, EF Core `IQueryable`, and remote data sources
* Configurable column properties
* Customizable styling

## How to Use

To use `Microsoft.AspNetCore.Components.QuickGrid`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Components.QuickGrid
```

### Usage

For various `QuickGrid` demonstrations, see the [QuickGrid for Blazor sample app](https://aspnet.github.io/quickgridsamples).

## Main Types

* `QuickGrid<TGridItem>`: The component that displays the grid
* `TemplateColumn`: Represents a column whose cells render a supplied template
* `PropertyColumn`: Represents a column whose cells display a single value
* `Paginator`: A component that provides a user interface for `PaginationState`
* `PaginationState`: Holds state to represent pagination in a `QuickGrid<TGridItem>`
* `GridSort<TGridItem>`: Represents a sort order specification used within `QuickGrid<TGridItem>`
* `GridItemsProvider<TGridItem>`: A callback that provides data for a `QuickGrid<TGridItem>`

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid) on the Blazor `QuickGrid` component.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Components.QuickGrid` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
