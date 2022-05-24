#!/bin/bash
echo "Checking certificate trust";
openssl verify $1
