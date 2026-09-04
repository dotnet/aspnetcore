### Auth security reviewer

Review only the ASP.NET Core auth/security area in `src/Security/**`, `src/Identity/**`, `src/DataProtection/**`, `src/Antiforgery/**`, and `src/WebEncoders/**`: authentication and authorization middleware, OAuth/OIDC, cookies, JWT bearer, Identity, DataProtection, antiforgery, claims, and encoding.

This file is reference material. The `review-pull-request` skill gives each dimension below an
independent, single-dimension pass.

Apply each CHECK to behavior the changed framework component owns or delegates. Trace the relevant
implementation, validators, and callbacks while preserving documented options, defaults, and event
contracts. Do not prescribe application deployment choices or require every handler to reimplement
validation owned by another component.

#### Overarching principles

- Prefer secure defaults over optional security. Authentication, cookies, antiforgery, token validation, key management, and redirect handling should be safe unless a caller explicitly opts into a documented weaker mode.
- Treat every external identity, token, redirect, and claim as untrusted until the appropriate cryptographic, protocol, and application validation has succeeded.
- Keep authentication state coherent. Schemes, options, tickets, principals, properties, cookies, nonces, correlation IDs, and claims must move together or fail together.
- Isolate secrets and protected data by purpose, lifetime, audience, and key ring. Never log, compare, cache, or expose secret material casually.
- Tests should prove observable security behavior and failure paths, not just successful helper execution.

#### Review dimensions

##### Scope, API shape, and compatibility

- CHECK: Keep auth/security behavior in the owned area; do not move protocol-library, database-provider, or app-specific policy decisions into shared ASP.NET Core infrastructure.
- CHECK: Prefer current authentication APIs such as direct `HttpContext` methods, scheme-specific options lookup, and modern default-scheme configuration; do not reintroduce legacy shims unless compatibility requires them.
- CHECK: Make collection and options surfaces communicate mutability: expose read-only policy/scheme maps unless callers intentionally mutate through a controlled method.

##### Cryptography, secrets, and key material

- CHECK: Use DataProtection with explicit purpose isolation for feature-level protected payloads; do not invent cryptographic schemes. For password hashing, secure randomness, protocol cryptography, and DataProtection internals, use the established platform or library abstractions appropriate to the operation.
- CHECK: Use `CryptographicOperations.FixedTimeEquals` for secret byte equality. Retain established compatibility helpers where the target framework lacks the API, and preserve specialized library-owned verification rather than replacing it with ordinary equality.
- CHECK: Keep key material, client secrets, credentials, tokens, recovery codes, and security stamps out of exceptions, logs, and telemetry. Prevent unintended disclosure through URLs or unrelated API results; preserve documented, purpose-specific issuance and retrieval contracts.
- CHECK: Generate nonces, correlation IDs, authenticators, and reset tokens with cryptographically secure randomness and protocol-appropriate entropy.
- CHECK: Validate certificate, key, and credential inputs at the boundary and fail closed when required material is missing, malformed, or incompatible; certificates used to encrypt new DataProtection keys must be valid and not expired, while expired certificates may be retained to decrypt historical keys.

##### DataProtection key ring and purpose isolation

- CHECK: For changes to DataProtection consumers, preserve explicit purpose isolation for each feature, scheme, token, and versioned payload; do not reuse protectors across unrelated data.
- CHECK: For changes to framework key management, preserve activation, expiration, revocation, rotation, propagation, and configured generation/fallback behavior; for changed in-repository stores, preserve the persistence contract consumed by key management. Do not prescribe application deployment choices or review unrelated external providers.
- CHECK: Use key-repository abstractions that are safe for their DI lifetime and create scoped or fresh persistence contexts as needed; never cache a mutable database context or XML document in singleton key-management paths.
- CHECK: Keep key persistence diagnostics useful but safe: log friendly key identifiers and storage context, skip malformed persisted entries deliberately, and avoid serializing secret key material.
- CHECK: Keep credential configuration bindable and minimal; load certificates or external credentials only in the component that uses them and validates their lifetime.
- CHECK: Maintain trimming and AOT compatibility for DataProtection packages by preserving required types without broad reflection roots.

##### Authentication schemes, options, and handler pipeline

- CHECK: Validate options during scheme initialization and per-use paths so invalid schemes consistently fail; include required options, key material, callback paths, and self-referential configuration.
- CHECK: Centralize forwarding resolution and prevent cycles across `ForwardAuthenticate`, `ForwardChallenge`, `ForwardForbid`, `ForwardSignIn`, `ForwardSignOut`, `ForwardDefaultSelector`, and `ForwardDefault`; allow self-reference only as an explicit opt-out.
- CHECK: Reject `SignInScheme`, default scheme, or handler forwarding settings that would recursively invoke the same handler unless the handler documents and tests the opt-out behavior.
- CHECK: Preserve handler ordering: request-handling callbacks that can short-circuit run before default authentication, and `Skipped` remains distinct from `Failed`.
- CHECK: Prefer `EventsType`-based event activation when both direct events and typed events exist, and keep event contexts exposing the handler state needed by extensibility.
- CHECK: Use idempotent service registration and scheme-specific `IOptionsMonitor<TOptions>.Get(scheme)` lookups so repeated registrations and dynamic schemes stay coherent.

##### Principals, tickets, claims, and Identity

- CHECK: Keep `AuthenticationTicket`, `AuthenticationProperties`, `ClaimsPrincipal`, and cookie/session metadata synchronized whenever validation events replace or mutate the principal.
- CHECK: Treat external claims and identities as untrusted input; validate issuer, authentication type, expected claim shape, and authenticated state before granting access.
- CHECK: Keep `IClaimsTransformation` implementations per-request, idempotent, scheme-aware, and safe to run multiple times; avoid caching or mutating shared principals or claims without a concrete lifetime and invalidation model.
- CHECK: Do not assume `HttpContext.User` is null for anonymous requests; check `Identity?.IsAuthenticated` and preserve anonymous principals when no scheme succeeds.
- CHECK: Preserve multi-scheme semantics by combining identities intentionally, documenting any first-identity-only serialization behavior, and retaining scheme metadata needed for challenge and sign-out.
- CHECK: Bound Identity security-stamp revalidation with a deliberate `ValidationInterval` and keep it distinct from cookie-ticket absolute expiration so stale authorization data is refreshed or rejected predictably.
- CHECK: Keep Identity UI, user-store interfaces, serialization contracts, and scaffolding customization points consistent and testable across get/post handlers.

##### Cookie, session, and SameSite security

- CHECK: Security-sensitive cookies use secure defaults: `HttpOnly`, `Secure` when appropriate for transport, explicit `SameSite`, scoped names/paths/domains, and minimal lifetime.
- CHECK: Authentication, Identity, nonce, correlation, antiforgery, and sign-in cookies must be essential when the feature cannot function securely under consent gating.
- CHECK: Preserve documented `SameSite` defaults and configured behavior, including supported cross-site login and navigation flows; do not treat `Strict` as a universal replacement for `Lax` or `None`.
- CHECK: Keep cookie ticket expiration, sliding renewal, and any concrete server-side auth-state cache lifetime aligned so cookies cannot outlive the validated authentication state.
- CHECK: Bind cookies to TLS token binding or equivalent transport-bound data only through null-safe feature access and stable Base64 encoding.
- CHECK: Assert cookie attributes in tests for sign-in, sign-out, external login, antiforgery, consent, HTTPS, and remote-callback flows.

##### OAuth, OIDC, and remote authentication protocols

- CHECK: For changed remote-authentication handlers or shared plumbing, trace the validation required by the supported protocol and flow through local checks, delegated validators, and events. Identify changed bypasses or weakening of the configured contract; do not apply OIDC-only requirements to OAuth-only flows or duplicate library-owned validation.
- CHECK: For OAuth/OIDC authorization-code flows, use PKCE where supported; generate high-entropy verifiers, store them only in protected correlation state, enforce documented challenge methods, and reject verifier replay, mismatch, or downgrade.
- CHECK: Build redirect URIs with framework helpers and reject open redirects; never concatenate untrusted hosts, paths, query strings, or return URLs into protocol redirects.
- CHECK: Keep `AuthenticationProperties.Parameters` and typed challenge properties for transient provider parameters; do not pollute serialized items or mutate dictionaries around serialization.
- CHECK: Distinguish transport failures from protocol failures and route them through targeted remote-error callbacks without exposing detailed provider errors to clients by default.
- CHECK: Support provider security features such as Pushed Authorization Requests when available while preserving interoperability and explicit opt-out behavior.
- CHECK: Validate token-endpoint response `Content-Type` before parsing JSON, and distinguish JSON UserInfo payloads from signed or encrypted JWT UserInfo responses with explicit malformed, empty, and unexpected-response paths.
- CHECK: Use `SkipUnrecognizedRequests`-style handling only for callback paths intentionally shared with unrelated endpoints, so unrelated requests do not become failed authentications solely because state or correlation fields are absent.
- CHECK: Keep unsolicited-login options such as `AllowUnsolicitedLogins` defaulted off for WS-Fed/SAML-style assertions; require explicit opt-in and tests because unsolicited assertions are XSRF/spoofing-prone.

##### JWT bearer and token validation

- CHECK: Trace changed token-validation paths through the configured token handlers, validation parameters, and events. Preserve format, signature, issuer, audience, lifetime, algorithm, signing-key, and replay validation as required by that contract before accepting a principal; do not require the bearer handler to reimplement library-owned checks.
- CHECK: Apply bounded, consistent clock skew to token, nonce, correlation, cookie, `nbf`, and `exp` validation; avoid unbounded or inconsistent grace periods.
- CHECK: Preserve `RequireHttpsMetadata` defaults and enforcement when changing metadata configuration or retrieval; distinguish that framework contract from application transport policy, issuer-granted scopes, and revocation unless the changed component owns those behaviors.
- CHECK: Null-check parsed token objects and validation results before reading claims or expiration metadata so malformed tokens fail clearly.
- CHECK: Keep token validation parameters explicit in samples, tests, and defaults; do not silently relax issuer, audience, lifetime, or signing-key validation for convenience.

##### Authorization policies and endpoint enforcement

- CHECK: Apply authorization through endpoint metadata and policy evaluation consistently; do not assume any auth marker implies `RequireAuthenticatedUser` unless the policy actually includes it.
- CHECK: Preserve default, fallback, named, combined, role, and permission policy semantics when middleware, endpoint routing, or metadata ordering changes.
- CHECK: Use explicit policy requirements for roles, scopes, permissions, and resource checks; avoid ad hoc claim checks that bypass authorization handlers.
- CHECK: Keep challenge and forbid behavior scheme-aware so authentication failure, authorization failure, multi-scheme policy, and anonymous access produce the expected response.
- CHECK: Validate authorization options early and expose policy collections consistently with authentication scheme collections.

##### Antiforgery and CSRF protection

- CHECK: Treat antiforgery as mandatory security infrastructure for cookie-authenticated state-changing requests; defaults should validate tokens rather than require apps to opt in.
- CHECK: Antiforgery cookie tokens use `HttpOnly`, `Secure` where appropriate, `SameSite=Strict`, essential-cookie behavior, and centralized token-store defaults; request tokens are form or header values that need safe transport, parsing, and logging behavior.
- CHECK: Bind antiforgery tokens to the authenticated user, security stamp, claim UID, or equivalent stable identity data so tokens cannot be replayed across users.
- CHECK: Validate tokens on unsafe HTTP methods and remote-login state transitions, and keep validation failures distinct from missing-authentication failures.
- CHECK: Cover missing, malformed, mismatched, anonymous, authenticated, consent-gated, and time-limited additional-data antiforgery cases with focused tests when an expiring additional-data provider is used.

##### Encoding, diagnostics, and validation

- CHECK: Use `WebEncoders` or context-appropriate platform encoders for HTML, URL, Base64Url, XML, JavaScript, and header contexts; never add custom encoding or string escaping for security data.
- CHECK: Validate and normalize authentication inputs at public boundaries, including scheme names, cookie names, callback paths, return URLs, token strings, credential options, and external payloads.
- CHECK: Error messages should tell developers how to fix configuration or preservation issues without revealing secrets, tokens, provider payloads, or internal stack traces to clients.
- CHECK: Use structured logging placeholders, stable event names, and `EventSource`/counter guards; place logs after successful operations when logging success would otherwise mask exceptions.
- CHECK: Respect cancellation such as `RequestAborted` in remote calls, token retrieval, and async long-running validation without treating client aborts as successful authentication.
- CHECK: Protect per-request auth hot paths by avoiding unnecessary allocations, buffering, and string churn in middleware, cookie/JWT parsing, `WebEncoders`, and DataProtection operations.
- CHECK: Add negative tests for malformed inputs, subtle certificate/key differences, protocol error mapping, logging redaction, trimming preservation, and security-sensitive defaults.
