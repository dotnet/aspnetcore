#!/usr/bin/env pwsh
#requires -version 4

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Import-Module -Name "$PSScriptRoot/Test-Template.psm1"

Test-Template `
    -TemplateName "mcpserver" `
    -TemplateArguments @("mcpserver") `
    -TemplatePackagePath "Microsoft.McpServer.ProjectTemplates.*-dev.nupkg" `
    -TemplateProjectPath "McpServer.ProjectTemplates/Microsoft.McpServer.ProjectTemplates.csproj" `
    -PublishArguments @("--runtime", "win-x64", "/p:RuntimeIdentifiers=win-x64")
