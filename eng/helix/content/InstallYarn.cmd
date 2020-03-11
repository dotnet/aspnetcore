set PATH=%HELIX_WORKITEM_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\jdk\bin
where npm
echo Contents of %HELIX_WORKITEM_ROOT%\node_modules
dir %HELIX_WORKITEM_ROOT%\node_modules
call npm.cmd i yarn
call yarn install
call npm.cmd install npm@latest -g
echo Contents of %HELIX_WORKITEM_ROOT%\node_modules
dir %HELIX_WORKITEM_ROOT%\node_modules
