set PATH=%HELIX_WORKITEM_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\jdk\bin
call npm.cmd i yarn
xcopy /S /I /E /Q %HELIX_WORKITEM_ROOT%\node_modules\npm npmBak
xcopy /S /I /E /Q %HELIX_WORKITEM_ROOT%\node_modules\yarn yarnBak
call yarn install
xcopy /S /I /E /Q npmBak %HELIX_WORKITEM_ROOT%\node_modules\npm
xcopy /S /I /E /Q yarnBak %HELIX_WORKITEM_ROOT%\node_modules\yarn
