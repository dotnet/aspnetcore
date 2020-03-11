set PATH=%HELIX_WORKITEM_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\jdk\bin
call npm.cmd i yarn
call yarn install
echo Contents of %HELIX_WORKITEM_ROOT%\node_modules
dir %HELIX_WORKITEM_ROOT%\node_modules
