#!/bin/bash
opensslMajor=$(openssl version | awk '{print $2}' | awk -F "." '{print $1}');
opensslMinor=$(openssl version | awk '{print $2}' | awk -F "." '{print $3}' | cut -c '2');
if [[  $opensslMajor -lt '3' && $opensslMinor < 'k' ]]; then
    echo "Insufficient OpenSSL version, the minimum required version is 1.1.1k";
    openssl version;
    exit -1;
fi