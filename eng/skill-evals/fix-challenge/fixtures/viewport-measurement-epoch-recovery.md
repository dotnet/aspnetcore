# Viewport measurement-epoch recovery fixture

## Accepted behavior

- Centering a timestamp temporarily owns viewport positioning.
- The first ordinary horizontal wheel event after centering transfers ownership
  back to the user without moving the earliest visible timestamp backward.
- Transient and settled renders must preserve a monotonic earliest timestamp.

## Changed path

A horizontally virtualized timeline/canvas disconnects its scroll and
`ResizeObserver` callbacks while `CenterOn` positions the requested timestamp.
During that interval, a web font finishes loading and a side panel resizes the
viewport. Both events change item widths and the leading extent.

On recovery, the first forward wheel event can consume:

- the leading extent captured before callback suppression;
- item widths measured after font load and panel resize;
- the new viewport width; and
- the current earliest rendered timestamp.

The current candidate reconnects callbacks without making these measurements an
atomic snapshot. A control implementation remeasures the leading extent, item
widths, and viewport width into one epoch before processing the first real wheel
or observer event.

## Existing tests

- `CenterOn_PreservesRequestedTimestamp` covers the centering operation.
- `PanForward_KeepsEarliestTimestampMonotonic` is unchanged and exercises
  ordinary forward panning through the shared viewport producer.
- Fixed-width and bounded variable-width timeline samples exist.
- No test changes geometry during the suppressed interval and then observes both
  the transient and settled states produced by the first real recovery event.
