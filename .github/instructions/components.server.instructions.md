---
description: Instructions for Components Server and Endpoints
applyTo: "src/Components/Server/**,src/Components/Endpoints/**"
---

## Logging and operational signals

* Before adding or promoting a log to `Warning`, `Error`, or `Critical`, trace all paths that can emit it, including paths driven by untrusted input. Treat changes to log severity as changes to operational behavior, not just diagnostic visibility.
* Do not emit alert-oriented logs solely because a client supplies invalid input, violates the protocol, requests an invalid state transition, or disconnects. Follow the area's established lower-severity logging conventions for these cases.
* Do not promote a shared catch-all log to diagnose one application failure when the same catch also handles client-input failures. Separate the failure categories and log application configuration errors or server defects at the appropriate severity. Client reachability alone does not make a genuine server failure a client-input error.
* When changing logging behavior, add coverage for both the intended application failure and representative invalid-client-input paths. Exercise the real parsing, validation, and dispatch path, and assert that invalid client input does not produce the new alert-oriented event.
