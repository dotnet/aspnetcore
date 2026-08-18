# Frozen policy-factory evidence

## Change

A parameter-policy factory wraps a policy differently when the parameter is
optional. The patch makes the optional wrapper implement the policy's outbound
transformation role. The required-parameter branch is unchanged.

The same policy instance implements both the inbound constraint role and the
outbound transformation role. Factory-created binders enumerate role-bearing
policies and retain role entries separately.

## Source contract

The downstream binder selects the first outbound transformer for a parameter.
The factory contract requires one effective outbound transformer for one policy
instance. Extra role entries are not an ordering mechanism.

## Retained base/head observations

The production binder path was exercised with an idempotent case-normalizing
transformer:

| Input shape | Base role calls | Head role calls | Base/head final value |
|---|---:|---:|---|
| Required value supplied | constraint 2, transform 2 | constraint 2, transform 2 | same normalized value |
| Optional value supplied | constraint 2, transform 0 | constraint 2, transform 2 | same normalized value |
| Optional value omitted | constraint 0, transform 0 | constraint 0, transform 0 | segment omitted |

The final value does not reveal whether normalization ran once or twice.

## Discriminating diagnostic

A counted transformer that appends its invocation ordinal was run on the same
production path. At head, the optional supplied case recorded two constraint
calls, two transform calls, and two appended ordinals. A candidate that hands
off only the effective outbound role recorded one call of each role and one
ordinal. Required and omitted cases were retained as boundary controls.

The patch does not change the required branch, but it makes the duplicated
outbound role reachable for the optional supplied input.
