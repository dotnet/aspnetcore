#!/bin/bash
echo "Trusting the certificate";
certificateName=$(basename $1);
opensslfolder=$(openssl version -d | awk '{ print $2}' | tr -d '"');
certificatePath="${opensslfolder}/certs/$certificateName";
cp $1 $certificatePath;
c_rehash;
