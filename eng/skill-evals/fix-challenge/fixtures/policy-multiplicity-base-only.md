# Frozen base-only multiplicity evidence

## Change

The patch changes the exception text produced when a required parameter value is
missing. It does not change policy construction, policy enumeration, outbound
transformation, or optional-parameter handling.

## Retained base/head observations

A dual-role policy on a supplied required parameter records two constraint
calls and two outbound transform calls on both base and head. Its idempotent
normalization produces the same final value on both revisions.

The changed exception branch runs only when the required value is omitted. That
branch exits before policy enumeration. The new message is covered by a focused
test, and source inspection confirms that no newly supplied or optional input is
routed through a different policy path.

## Scope

The duplicate callbacks may merit separate investigation, but the same causal
path, call multiplicity, and final behavior exist on base. The patch neither
makes that path reachable for a new input/configuration nor changes its
multiplicity.
