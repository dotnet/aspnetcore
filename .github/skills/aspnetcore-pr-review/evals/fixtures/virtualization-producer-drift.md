# Browser virtualization producer-drift fixture

## Review history

The original local review completed at frozen head A. A later commit changes
the shared `IntersectionObserver` batch producer.

## Later-head diff

```typescript
const bothSpacersIntersect = entries.some(isBefore) && entries.some(isAfter);
const entriesToDispatch = entries.filter(entry =>
  !(bothSpacersIntersect && entry.target === spacerAfter));
```

The edited startup test verifies that one callback is dispatched when both
spacers are visible during initial observation.

## Unchanged consumers and status

- `ProgrammaticScrollToBottom_ReachesLastItems` sets `scrollTop` to
  `scrollHeight` and waits for a rendered item index above 480.
- Tail loading is initiated by the after-spacer callback.
- The test passed at frozen head A.
- The current Components E2E run fails that unchanged test in CoreCLR and both
  Mono execution modes.

