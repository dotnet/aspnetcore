set PATH=%PATH%;%HELIX_CORRELATION_PAYLOAD%\jdk\bin;%HELIX_CORRELATION_PAYLOAD%\node\bin
echo Contents of %HELIX_CORRELATION_PAYLOAD%\jdk\bin
dir %HELIX_CORRELATION_PAYLOAD%\jdk\bin
echo Contents of %HELIX_CORRELATION_PAYLOAD%\node\bin
dir %HELIX_CORRELATION_PAYLOAD%\node\bin
call npm.cmd i yarn
call npm.cmd install npm@latest -g
call yarn install
