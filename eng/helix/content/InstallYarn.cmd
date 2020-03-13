set PATH=%HELIX_WORKITEM_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\jdk\bin

echo Contents of %HELIX_WORKITEM_ROOT%\node_modules
dir %HELIX_WORKITEM_ROOT%\node_modules

call npm.cmd i yarn
echo Contents of %HELIX_WORKITEM_ROOT%\node_modules after npm i yarn
dir %HELIX_WORKITEM_ROOT%\node_modules

call yarn install --verbose

echo Contents of %HELIX_WORKITEM_ROOT%\node_modules after yarn install
dir %HELIX_WORKITEM_ROOT%\node_modules
