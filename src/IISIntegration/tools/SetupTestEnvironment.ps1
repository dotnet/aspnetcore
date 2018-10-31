param($Mode)

$DumpFolder = "$env:ASPNETCORE_TEST_LOG_DIR\dumps"
if (!($DumpFolder))
{
    $DumpFolder = "$PSScriptRoot\..\artifacts\dumps"
}
if (!(Test-Path $DumpFolder))
{
    New-Item $DumpFolder -ItemType Directory;
}
$DumpFolder = Resolve-Path $DumpFolder

$LogsFolder = "$PSScriptRoot\..\artifacts\logs"
if (!(Test-Path $LogsFolder))
{
    New-Item $LogsFolder -ItemType Directory;
}
$LogsFolder = Resolve-Path $LogsFolder

$werHive = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting";
$ldHive = "$werHive\LocalDumps";


function Setup-appverif($application)
{
    appverif.exe -enable Exceptions Handles Heaps Leak Locks Memory Threadpool TLS SRWLock -for $application
    $level = 0x1E1;
    $codes = @(
        # Exceptions
        0x650,
        # Handles
        0x300, 0x301, 0x302, 0x303, 0x304, # 0x305,
        # Heaps
        0x001, 0x002, 0x003, 0x004, 0x005, 0x006, 0x007, 0x008, 0x009, 0x00A, 0x00B, 0x00C, 0x00D, 0x00E, 0x00F, 0x010, 0x011, 0x012, 0x013, 0x014,
        # Leak
        0x900, 0x901, 0x902, 0x903, 0x904, 0x905, 0x906,
        # Locks
        0x200, 0x201, 0x202, 0x203, 0x204, 0x205, 0x206, 0x207, 0x208, 0x209, 0x210, 0x211, 0x212, 0x213, 0x214, 0x215,
        # Memory
        0x600, 0x601, 0x602, 0x603, 0x604, 0x605, 0x606, 0x607, 0x608, 0x609, 0x60A, 0x60B, 0x60C, 0x60D, 0x60E, 0x60F, 0x610, 0x612, 0x613, 0x614, 0x615, 0x616, 0x617, 0x618, 0x619, 0x61A, 0x61B, 0x61C, 0x61D, 0x61E,
        # SRWLock
        0x250, 0x251, 0x252, 0x253, 0x254, 0x255, 0x256, 0x257,
        # TSL
        0x350, 0x351, 0x352,
        # ThreadPool
        0x700, 0x701, 0x702, 0x703, 0x704, 0x705, 0x706, 0x707, 0x708, 0x709, 0x70A, 0x70B, 0x70C, 0x70D
    );

    setx APPVERIFIER_ENABLED_CODES "$codes";
    setx APPVERIFIER_LEVEL $level;
    appverif.exe -configure $codes -for $application -with ErrorReport=$level

    # 0x305, - disabled because coreclr.dll!SetThreadName(void *) ofthen passes invalid handle (0xffffff)
    appverif.exe -configure 0x305 -for $application -with ErrorReport=0
}

function Shutdown-appverif($application)
{
    setx APPVERIFIER_ENABLED_CODES "NONE";
    setx APPVERIFIER_LEVEL "NONE";

    appverif.exe -disable * -for $application
}

function Setup-Dumps()
{
    if (!(Test-Path $ldHive ))
    {
        New-Item -Path $werHive -Name LocalDumps
    }

    Move-Item $env:windir\System32\vsjitdebugger.exe $env:windir\System32\_vsjitdebugger.exe;

    New-ItemProperty $werHive -Name "DontShowUI" -Value 1 -PropertyType "DWORD" -Force;

    New-ItemProperty $ldHive -Name "DumpFolder" -Value $DumpFolder -PropertyType "ExpandString" -Force;
    New-ItemProperty $ldHive -Name "DumpCount" -Value 15 -PropertyType "DWORD" -Force;
    New-ItemProperty $ldHive -Name "DumpType" -Value 2 -PropertyType "DWORD" -Force;

    Restart-Service WerSvc
}

function Shutdown-Dumps()
{
    Move-Item $env:windir\System32\_vsjitdebugger.exe $env:windir\System32\vsjitdebugger.exe;

    Remove-Item $ldHive -Recurse -Force

    New-ItemProperty $werHive -Name "DontShowUI" -Value 0 -PropertyType "DWORD" -Force;

    $cdb = "c:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"
    if (!(Test-Path $cdb))
    {
        $downloadedFile = [System.IO.Path]::GetTempFileName();
        $downloadedFile = "$downloadedFile.exe";
        Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/p/?linkid=870807" -OutFile $downloadedFile;
        & $downloadedFile /features OptionId.WindowsDesktopDebuggers /norestart /q;
    }

    foreach ($dump in (Get-ChildItem -Path $DumpFolder -Filter "*.dmp"))
    {
        if (Test-Path $cdb)
        {
            & $cdb -z $dump.FullName -y "https://msdl.microsoft.com/download/symbols" -c ".loadby sos coreclr;!sym noisy;.reload /f;.dumpcab -a $($dump.FullName).cab;q;"
            Remove-Item $dump.FullName
        }
    }
}

if ($Mode -eq "Setup")
{
    Setup-appverif w3wp.exe
    Setup-appverif iisexpress.exe

    Setup-Dumps;
}

if ($Mode -eq "SetupDumps")
{
    Shutdown-appverif w3wp.exe
    Shutdown-appverif iisexpress.exe

    Setup-Dumps;
}

if ($Mode -eq "Shutdown")
{
    Shutdown-appverif w3wp.exe
    Shutdown-appverif iisexpress.exe

    Shutdown-Dumps;
}

Exit 0;