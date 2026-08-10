# Browser virtualization recovery fixture

## Accepted behavior

- A positive initial item remains authoritative through unrelated layout
  activity until an explicit navigation action transfers ownership.
- Ordinary scrolling after initial positioning must not move the visible range
  backward.

## Changed path

The patch changes how `IntersectionObserver` callbacks are classified and
ignored while JavaScript positions the initial item. Ignored callbacks return
before the .NET measurement consumer commits the observed spacer and row data.

The next processed callback supplies:

- a spacer pixel size from the DOM;
- rendered-row separation from the current DOM batch;
- the current item count; and
- an average item size recomputed by `ProcessMeasurements`.

`CalculateItemDistribution` then divides spacer pixels by the committed average
item size to select the next logical window.

## Existing tests

- Initial placement at `InitialItemIndex = 500`.
- Home/End ownership transfer after initial placement.
- Fixed-height and variable-height rendering samples.
- No test performs ordinary scrolling until the opposite spacer becomes
  visible after the ignored-callback interval.

