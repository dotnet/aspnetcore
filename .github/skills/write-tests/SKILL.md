---
name: write-tests
description: Creates unit tests for GitHub issues in dotnet/aspnetcore. Determines the appropriate test project and writes xUnit tests that reproduce the bug. Use when asked to "write tests", "create tests", "add tests for issue #XXXXX", or when the fix-issue skill needs tests created.
---

# Write Tests for ASP.NET Core Issues

Creates unit tests that reproduce a bug described in a GitHub issue.

## When to Use

- When a fix has no tests covering the bug
- When the `fix-issue` skill's Gate phase finds no tests
- When explicitly asked to write tests for an issue

## Workflow

### Step 1: Understand the Bug

Read the issue description and identify:
- What the expected behavior is
- What the actual (buggy) behavior is
- Reproduction steps
- The area under `src/` affected

### Step 2: Find the Test Project

```bash
# Find test projects in the area
find src/{Area} -name "*Test*.csproj" -o -name "*Tests*.csproj"

# Find existing test files for style reference
find src/{Area}/test -name "*.cs" | head -20
```

### Step 3: Find Similar Tests

```bash
# Search for tests related to the buggy component
grep -r "class.*Tests" src/{Area}/test/ --include="*.cs" -l
grep -r "{BuggyClassName}" src/{Area}/test/ --include="*.cs" -l
```

### Step 4: Write the Tests

Follow these conventions:
- **Framework:** xUnit SDK v3
- **Mocking:** Moq
- **Style:** Copy naming conventions from nearby test files
- **No comments:** Do not add `// Arrange`, `// Act`, `// Assert`
- **Nullable:** Use `is null` / `is not null`

### Step 5: Verify Tests Fail

```bash
# Activate .NET environment
source activate.sh

# Run just the new tests — they should FAIL against buggy code
dotnet test src/{Area}/test/{TestProject}.csproj --filter "FullyQualifiedName~{NewTestClass}"
```

### Step 6: For Java Projects (SignalR Java Client)

```bash
cd src/SignalR/clients/java/signalr
./gradlew test
```

## Test Template (C#)

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.{Area}.Tests;

public class {ClassName}Tests
{
    [Fact]
    public void {MethodName}_{Scenario}_{ExpectedResult}()
    {
        var sut = new {ClassName}();

        var result = sut.{Method}({input});

        Assert.Equal({expected}, result);
    }
}
```

## Test Template (Java)

```java
@Test
public void {testMethodName}() {
    MockTransport mockTransport = new MockTransport();
    HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

    hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

    // Test the buggy behavior
    // ...

    // Assert expected behavior
    assertEquals(expected, actual);
}
```

## Output

Report which tests were created, which test project they're in, and whether they fail against the current buggy code.
