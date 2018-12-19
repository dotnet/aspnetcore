set target=%1
set sdkVersion=%2
set runtimeVersion=%3
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Version %sdkVersion% -InstallDir %HELIX_CORRELATION_PAYLOAD%\sdk"
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Runtime dotnet -Version %runtimeVersion% -InstallDir %HELIX_CORRELATION_PAYLOAD%\sdk"
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_ROOT="$HELIX_CORRELATION_PAYLOAD/sdk"
set PATH="$DOTNET_ROOT:$PATH"
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME="$HELIX_CORRELATION_PAYLOAD/home"
%HELIX_CORRELATION_PAYLOAD%\sdk\dotnet vstest %target% --logger:trx


