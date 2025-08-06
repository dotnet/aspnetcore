# CI Template Tests Fix Summary

## Original Issue
The CI job at `.azure/pipelines/ci-public.yml:632` was disabled due to conflicts with concurrent template tests, causing build failures when both the local development validation and Helix template tests ran simultaneously.

## Root Cause Analysis

### Conflict Sources
1. **Global Template Installation Conflicts**
   - Helix template tests used isolated custom hive (`--debug:custom-hive`)
   - Blazor web script installed templates globally in user profile
   - Concurrent install/uninstall operations caused race conditions

2. **Shared Template Engine State**
   - Both workflows accessed the same global template engine state
   - Template packages were installed/uninstalled without isolation
   - Led to "template not found" or "template already installed" errors

3. **Environment Variable Pollution**
   - Blazor web script modified global `DOTNET_ROOT`, `DOTNET_ROOT_X86`, and `Path`
   - Could interfere with concurrent test execution environments

## Solution Implemented

### Template Isolation Strategy
Adopted the same isolation pattern already proven in Helix template tests:

#### Before (Conflicting):
```powershell
# Global template installation
dotnet new install $PackagePath
# Global template usage  
dotnet new $templateName --no-restore
```

#### After (Isolated):
```powershell
# Unique hive per test run
$CustomHivePath = "$PSScriptRoot/.templatehive-$templateName"
# Isolated installation
dotnet new install $PackagePath --debug:disable-sdk-templates --debug:custom-hive $CustomHivePath
# Isolated usage
dotnet new $templateName --no-restore --debug:custom-hive $CustomHivePath --debug:disable-sdk-templates
```

### Key Components
1. **Unique Custom Hive**: `$PSScriptRoot/.templatehive-$templateName` per test
2. **SDK Template Exclusion**: `--debug:disable-sdk-templates` prevents interference
3. **Automatic Cleanup**: Custom hive removed in `finally` block
4. **Process-Scoped Environment**: Variables scoped to avoid global pollution

## Files Modified
- `src/ProjectTemplates/scripts/Test-Template.psm1`: Added template isolation
- `.azure/pipelines/ci-public.yml`: Re-enabled disabled job (condition: 'false' → 'true')

## How to Test Similar Issues in the Future

### 1. Identify Conflicts
```bash
# Check for global template operations
grep -r "dotnet new install" --exclude-dir=.git
grep -r "\.templateengine" --exclude-dir=.git

# Check for environment variable modifications
grep -r "env:DOTNET_ROOT" --exclude-dir=.git
grep -r "env:Path" --exclude-dir=.git
```

### 2. Implement Isolation
- Use `--debug:custom-hive` for template operations
- Use unique directory names per test run
- Clean up custom hives in finally blocks
- Scope environment variables to process level

### 3. Validate Solution
- Run both workflows individually to ensure they still work
- Run both workflows concurrently to verify no conflicts
- Check CI logs for template-related errors

## Monitoring Points for Re-enabled Functionality

### Success Indicators
- Both template test jobs complete successfully
- No "template not found" errors in logs  
- No "template already installed" errors in logs
- No timeout issues in template operations
- Proper artifact flow between jobs

### Warning Signs
- Template installation/uninstallation errors
- Test isolation failures
- Environment variable conflicts
- Resource access conflicts
- Concurrent access exceptions

## Technical Notes

### Template Engine Isolation
The `--debug:custom-hive` flag creates a completely isolated template engine instance:
- Templates installed in custom hive don't affect global template state
- Multiple custom hives can operate simultaneously without conflicts
- Each hive maintains its own template package registry

### Command Syntax
The solution uses the proven command pattern from existing template tests:
```powershell
dotnet new {command} --debug:disable-sdk-templates --debug:custom-hive "{path}"
```

This ensures compatibility with the existing template test infrastructure while providing complete isolation.