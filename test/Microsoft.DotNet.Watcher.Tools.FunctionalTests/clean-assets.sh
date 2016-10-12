#!/usr/bin/env bash

if [ -z $1 ]; then
    echo "Deleting $1/TestProjects"
    rm -rf $1/TestProjects
fi

exit 0