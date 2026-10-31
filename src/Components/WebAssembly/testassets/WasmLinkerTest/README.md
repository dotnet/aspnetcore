## Trimmer baseline verification

This project is used to verify that Blazor WASM APIs do not result in additional trimmer warnings other than the ones that are already known. It works by running the trimmer in "library mode", rooting all of it's public APIs, using a set of baselined suppressions and ensuring no new trimmer warnings are produced.

### Updating the baselines

If a suppressed warning has been resolved, or if new trimmer warnings are to be baselined, run the following command:

```
dotnet build /p:GenerateLinkerWarningSuppressions=true
```

This should update the WarningSuppressions.xml files associated with projects.

⚠️ Note that the generated file sometimes messing up formatting for some compiler generated nested types and you may need to manually touch up these files on regenerating. The generated file uses braces `{...}` instead of angle brackets `<...>`:

```diff
- LegacyRouteTableFactory.&lt;&gt;c.{Create}b__2_1(System.Reflection.Assembly)
+ LegacyRouteTableFactory.&lt;&gt;c.&lt;Create&gt;b__2_1(System.Reflection.Assembly)
```
