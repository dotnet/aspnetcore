---
description: Instructions for Components Server and Endpoints
applyTo: "src/Components/Server/**,src/Components/Endpoints/**"
---

## Logging and operational signals

* Before adding or promoting a log to `Warning`, `Error`, or `Critical`, trace all paths that can emit it, including paths driven by untrusted input. Treat changes to log severity as changes to operational behavior, not just diagnostic visibility.
* Do not emit alert-oriented logs solely because a client supplies invalid input, violates the protocol, requests an invalid state transition, or disconnects. Follow the area's established lower-severity logging conventions for these cases.
* Do not promote a shared catch-all log to diagnose one application failure when the same catch also handles client-input failures. Separate the failure categories and log application configuration errors or server defects at the appropriate severity. Client reachability alone does not make a genuine server failure a client-input error.
* When changing logging behavior, add coverage for both the intended application failure and representative invalid-client-input paths. Exercise the real parsing, validation, and dispatch path, and assert that invalid client input does not produce the new alert-oriented event.

## Circuit ordering review checks

* When serializing root-component update batches, distinguish operation ordering from initial-render quiescence. `CircuitHost` waits for the first batch's initial renders; queuing later Remove/Add batches behind that full wait can deadlock when a component's `OnInitializedAsync` completes only after navigation disposes it. Verify that the removal needed to release initialization can still run.
* Trace `CircuitHost.UnhandledException` subscribers before changing client-error notification order. `CircuitRegistry` handles the event by calling `TerminateAsync`, which calls `Client.SetDisconnected()` synchronously for a registered circuit before returning the disposal `ValueTask` that the event handler awaits. `TryNotifyClientErrorAsync` skips disconnected clients, so raising the event first can suppress the intended `JS.Error` notification.
