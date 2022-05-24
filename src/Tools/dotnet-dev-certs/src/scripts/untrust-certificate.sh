#!/bin/bash
echo "Untrusting the certificate";
opensslfolder=$(openssl version -d | awk '{ print $2}' | tr -d '"');
certificatePath="${opensslfolder}/certs/$1";
rm $certificatePath
c_rehash
