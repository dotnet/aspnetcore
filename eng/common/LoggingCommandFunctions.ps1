# Source for this file was taken from https://github.com/microsoft/azure-pipelines-task-lib/blob/11c9439d4af17e6475d9fe058e6b2e03914d17e6/powershell/VstsTaskSdk/LoggingCommandFunctions.ps1

# NOTE: You should not be calling these method directly as they are likely to change.  Instead you should be calling the Write-Pipeline* functions defined in tools.ps1

$script:loggingCommandPrefix = '##vso['
$script:loggingCommandEscapeMappings = @( # TODO: WHAT ABOUT "="? WHAT ABOUT "%"?
    New-Object psobject -Property @{ Token = ';' ; Replacement = '%3B' }
    New-Object psobject -Property @{ Token = "`r" ; Replacement = '%0D' }
    New-Object psobject -Property @{ Token = "`n" ; Replacement = '%0A' }
    New-Object psobject -Property @{ Token = "]" ; Replacement = '%5D' }
)
# TODO: BUG: Escape % ???
# TODO: Add test to verify don't need to escape "=".

<########################################
# Private functions.
########################################>
function Format-LoggingCommandData {
    [CmdletBinding()]
    param([string]$Value, [switch]$Reverse)

    if (!$Value) {
        return ''
    }

    if (!$Reverse) {
        foreach ($mapping in $script:loggingCommandEscapeMappings) {
            $Value = $Value.Replace($mapping.Token, $mapping.Replacement)
        }
    } else {
        for ($i = $script:loggingCommandEscapeMappings.Length - 1 ; $i -ge 0 ; $i--) {
            $mapping = $script:loggingCommandEscapeMappings[$i]
            $Value = $Value.Replace($mapping.Replacement, $mapping.Token)
        }
    }

    return $Value
}

function Format-LoggingCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Area,
        [Parameter(Mandatory = $true)]
        [string]$Event,
        [string]$Data,
        [hashtable]$Properties)

    # Append the preamble.
    [System.Text.StringBuilder]$sb = New-Object -TypeName System.Text.StringBuilder
    $null = $sb.Append($script:loggingCommandPrefix).Append($Area).Append('.').Append($Event)

    # Append the properties.
    if ($Properties) {
        $first = $true
        foreach ($key in $Properties.Keys) {
            [string]$value = Format-LoggingCommandData $Properties[$key]
            if ($value) {
                if ($first) {
                    $null = $sb.Append(' ')
                    $first = $false
                } else {
                    $null = $sb.Append(';')
                }

                $null = $sb.Append("$key=$value")
            }
        }
    }

    # Append the tail and output the value.
    $Data = Format-LoggingCommandData $Data
    $sb.Append(']').Append($Data).ToString()
}

function Write-LoggingCommand {
    [CmdletBinding(DefaultParameterSetName = 'Parameters')]
    param(
        [Parameter(Mandatory = $true, ParameterSetName = 'Parameters')]
        [string]$Area,
        [Parameter(Mandatory = $true, ParameterSetName = 'Parameters')]
        [string]$Event,
        [Parameter(ParameterSetName = 'Parameters')]
        [string]$Data,
        [Parameter(ParameterSetName = 'Parameters')]
        [hashtable]$Properties,
        [Parameter(Mandatory = $true, ParameterSetName = 'Object')]
        $Command,
        [switch]$AsOutput)

    if ($PSCmdlet.ParameterSetName -eq 'Object') {
        Write-LoggingCommand -Area $Command.Area -Event $Command.Event -Data $Command.Data -Properties $Command.Properties -AsOutput:$AsOutput
        return
    }

    $command = Format-LoggingCommand -Area $Area -Event $Event -Data $Data -Properties $Properties
    if ($AsOutput) {
        $command
    } else {
        Write-Host $command
    }
}

function Write-LogIssue {
    [CmdletBinding()]
    param(
        [ValidateSet('warning', 'error')]
        [Parameter(Mandatory = $true)]
        [string]$Type,
        [string]$Message,
        [string]$ErrCode,
        [string]$SourcePath,
        [string]$LineNumber,
        [string]$ColumnNumber,
        [switch]$AsOutput)

    $command = Format-LoggingCommand -Area 'task' -Event 'logissue' -Data $Message -Properties @{
            'type' = $Type
            'code' = $ErrCode
            'sourcepath' = $SourcePath
            'linenumber' = $LineNumber
            'columnnumber' = $ColumnNumber
        }
    if ($AsOutput) {
        return $command
    }

    if ($Type -eq 'error') {
        $foregroundColor = $host.PrivateData.ErrorForegroundColor
        $backgroundColor = $host.PrivateData.ErrorBackgroundColor
        if ($foregroundColor -isnot [System.ConsoleColor] -or $backgroundColor -isnot [System.ConsoleColor]) {
            $foregroundColor = [System.ConsoleColor]::Red
            $backgroundColor = [System.ConsoleColor]::Black
        }
    } else {
        $foregroundColor = $host.PrivateData.WarningForegroundColor
        $backgroundColor = $host.PrivateData.WarningBackgroundColor
        if ($foregroundColor -isnot [System.ConsoleColor] -or $backgroundColor -isnot [System.ConsoleColor]) {
            $foregroundColor = [System.ConsoleColor]::Yellow
            $backgroundColor = [System.ConsoleColor]::Black
        }
    }

    Write-Host $command -ForegroundColor $foregroundColor -BackgroundColor $backgroundColor
}