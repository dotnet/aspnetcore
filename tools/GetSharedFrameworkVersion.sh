#!/bin/bash
dotnet --info | grep -i version | tail -1 | cut -f 2 -d ":" | tr -d ' '