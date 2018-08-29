#!/bin/sh

dotnet test --logger "console;verbosity=detailed" "$@"
