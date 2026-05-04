# Response Guidelines

How to write the `proposedResponse.body` Markdown GitHub comment after reproduction.

## Tone and Evidence

- **Tone:** Professional, helpful, empathetic. Thank the reporter.
- **Evidence:** Cite specific versions tested, platforms, and behaviors observed.
- **Workarounds:** If discovered during reproduction, include prominently.

## Status Thresholds

| Status | When |
|--------|------|
| `ready` | Confidence ≥ 0.85 — comment can be posted as-is |
| `needs-human-edit` | Confidence < 0.85, or sensitive/ambiguous situation |
| `do-not-post` | Internal-only findings, not suitable for public comment |

## Templates by Conclusion

### Reproduced — fixed on latest

```markdown
Thanks for reporting this! We were able to reproduce the issue on .NET {reporter_version}.

The good news is that this appears to be fixed in .NET {fixed_version}. Could you try upgrading?

**Tested versions:**
| Version | Result |
|---------|--------|
| .NET {reporter_version} | ❌ Reproduced |
| .NET {fixed_version} | ✅ Fixed |

{workaround_section_if_any}
```

### Reproduced — still present in latest

```markdown
We've confirmed this issue. The reported behavior reproduces on .NET {reporter_version} and {latest_version}.

We're tracking this for investigation. {workaround_section_if_any}

{missing_info_request_if_any}
```

### Not reproduced — requesting info

```markdown
Thanks for the report. We attempted to reproduce this on {environment} with .NET {reporter_version} but were unable to observe the described behavior.

Could you share the following to help us investigate?
{missing_info_list}

We'll take another look once we have more information.
```

### Not reproduced — works on all tested versions

```markdown
Thanks for the report. We tested this on .NET {reporter_version} and {latest_version} on {platform} but could not reproduce the issue.

**Tested versions:**
| Version | Result |
|---------|--------|
| .NET {reporter_version} | ✅ Works |
| .NET {latest_version} | ✅ Works |

Could you share:
- The output of `dotnet --info` from your environment
- A minimal reproduction project (see https://github.com/dotnet/aspnetcore/blob/main/docs/repro.md)

This will help us understand if there's an environment-specific factor at play.
```

### Workaround section template

```markdown
**Workaround** (available while a fix is investigated):

{workaround_description}

```csharp
{workaround_code}
```
```
