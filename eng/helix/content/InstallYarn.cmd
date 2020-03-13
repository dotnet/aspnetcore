set PATH=%HELIX_WORKITEM_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\jdk\bin

call npm.cmd i yarn
echo Contents of %HELIX_WORKITEM_ROOT%\node_modules after npm i yarn
dir %HELIX_WORKITEM_ROOT%\node_modules

xcopy /S /I /E %HELIX_WORKITEM_ROOT%\node_modules\npm npmBak
xcopy /S /I /E %HELIX_WORKITEM_ROOT%\node_modules\yarn yarnBak

call yarn install

xcopy /S /I /E npmBak %HELIX_WORKITEM_ROOT%\node_modules\npm
xcopy /S /I /E yarnBak %HELIX_WORKITEM_ROOT%\node_modules\yarn

echo Contents of %HELIX_WORKITEM_ROOT%\node_modules after yarn install
dir %HELIX_WORKITEM_ROOT%\node_modules

