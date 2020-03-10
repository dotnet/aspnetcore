set PATH=%PATH%;%25HELIX_CORRELATION_PAYLOAD%25\jdk\bin
call npm.cmd i yarn
call npm.cmd install npm@latest -g
call yarn install
