# Accessibility and localization

Applies to `A11Y-*`.

## Keep evidence layers separate

Record each layer independently:

1. source roles, properties, keyboard handlers, and CSS;
2. automated scanner output;
3. browser interaction behavior;
4. assistive-technology behavior;
5. formal conformance assessment;
6. maintainer attestation.

Source evidence can justify targeted browser probes. It cannot establish WCAG conformance.

## Component exercise

- Identify the applicable WAI-ARIA pattern and expected keyboard interaction.
- Exercise pointer and keyboard operation, focus entry/exit, roving focus, restoration, validation,
  selection, expansion, and asynchronous states.
- Inspect the computed accessibility tree, not only rendered attributes.
- Test supported LTR/RTL behavior and localized user-facing strings.
- Exercise Windows High Contrast or forced-colors behavior.
- Request recorded screen-reader coverage for the claimed support matrix.
- Use a suitable automated scanner and full assessment method for the target environment.
  Accessibility Insights FastPass and Full Assessment are qualifying examples, not required brands.

## Scoring boundaries

- Score reproducible semantic, keyboard, focus, announcement, localization, or contrast failures as
  `defect`.
- Score missing automated-scan, full-assessment, screen-reader, or conformance records as
  `maintainer evidence required`.
- Use `not tested` when the reviewer could perform a relevant probe but did not.
- Do not score `A11Y-01` verified from ARIA source, an axe scan, or a single keyboard smoke test.
- A scanner pass does not override a deterministic browser or assistive-technology failure.

## Evidence quality

Every accessibility claim should name the control state, render mode, browser, interaction, and
expected/observed accessible behavior. Formal evidence should identify version, configuration,
exceptions, date, and scope.
