set PATH=%PATH%;.\jdk\bin
call npm.cmd i yarn
call npm.cmd install npm@latest -g
call yarn install
