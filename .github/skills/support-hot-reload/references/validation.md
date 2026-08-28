# Validating that hot reload works

An invalidation is only proven when a live edit takes effect **without a restart**. The critical pitfall: if the tooling is configured to restart on a rude edit, a broken hot reload silently falls back to a restart and the edited output still appears — so an assertion that only checks the output passes even when hot reload failed. Validation must assert the mechanism, not just the result.

## Drive the edit under a watcher

Run the app under `dotnet watch` (launched so the app uses the runtime under test), reach the state you want to observe, then edit the source file that declares the thing your cache is keyed on (add the `[Parameter]`, the `[CascadingParameter]`, the `@page`, the `[JSInvokable]`). Capture the full watch output at verbose/trace so the hot-reload machinery is logged; the validation artifact is the saved watch log plus the edited source diff and the observed output/DOM before and after the edit.

```powershell
New-Item -ItemType Directory -Force -Path .\artifacts | Out-Null
dotnet watch --project <app-project> --verbose run *>&1 | Tee-Object -FilePath .\artifacts\hot-reload-watch.log
```

## Assert on the watch log, and on no restart

Confirm two things from the captured log:

1. The delta was applied in place — the watcher logs a hot-reload-applied message (an "changes applied" / "Updates applied" style line). Pin the exact strings from a real run against your toolchain; they vary by version.
2. The app did not restart — count a once-per-launch marker (for example the app's "Now listening on:" startup line) before and after the edit and confirm it did not increase. A rude edit instead logs a rude-edit/restart sequence and the launch marker count goes up.

Then confirm the observable behavior reflects the edit: for server-rendered output, poll the response body and save the before/after snippets; for interactive Blazor, poll the live DOM and save the before/after text or screenshot (the component re-renders in place with no page reload).

## Distinguish the three outcomes

- Supported edit, correct invalidation: hot-reload-applied logged, no restart, new behavior visible.
- Supported edit, missing invalidation: hot-reload-applied logged, no restart, but the behavior does not change (the stale cache is still used). This is the assertion that catches a missing `ClearCache` — the delta applied, yet the feature did not react.
- Rude edit: rude-edit/restart logged, launch marker increments; the behavior changes only because the app restarted. Assert the restart, not an in-place apply.

## Cover both runtimes

For a feature that runs under both Blazor Server and Blazor WebAssembly, validate on each: the same edit can hot-reload on Server (CoreCLR) yet be a rude edit on WebAssembly (Mono). For WebAssembly, keep a browser connected for the duration, since the delta is delivered over the browser connection.

## Completion

The feature's hot-reload support is validated when, for each metadata edit the feature cares about, the validation artifacts show the new behavior with a hot-reload-applied log and no restart, and the diff contains the cache invalidation plus any required refresh signal (`ClearCache`, `OnDeltaApplied`, change-token cancellation, endpoint rebuild, or re-render trigger). If an edit is inherently a rude edit, the validated outcome is a clean restart, documented as a limit rather than a bug.
