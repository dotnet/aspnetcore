#!/usr/bin/env bash

#
# Should be obvious, but don't use the certs created here for anything real. This is just meant for our testing.
#

set -euo pipefail

__machine_has() {
    hash "$1" > /dev/null 2>&1
    return $?
}

#
# Main
#

if ! __machine_has openssl; then
	echo 'OpenSSL is required to create the test certificates.' 1>&2
	exit 1
fi

# See https://www.openssl.org/docs/man1.0.2/apps/x509.html for more details on the openssl conf file

if [[ $# == 0 ]]; then
	echo "Usage: ${BASH_SOURCE[0]} <INI_FILE>..."
	echo ""
	echo "Arguments:"
	echo "  <INI_FILE>      Multiple allowed. Path to the *.ini file that configures a cert."
fi

# loop over all arguments
while [[ $# > 0 ]]; do
	# bashism for trimming the extension
	config=$1
	shift
	cert_name="${config%.*}"
	key="$cert_name.pem"
	cert="$cert_name.crt"
	pfx="$cert_name.pfx"

	echo "Creating cert $cert_name"

	# see https://www.openssl.org/docs/man1.0.2/apps/req.html
	openssl req -x509 \
		-days 1 \
		-config $config \
		-nodes \
		-newkey rsa:2048 \
		-keyout $key \
		-extensions req_extensions \
		-out $cert

	# See https://www.openssl.org/docs/man1.0.2/apps/pkcs12.html
	openssl pkcs12 -export \
		-in $cert \
		-inkey $key \
		-out $pfx \
		-password pass:testPassword # so secure ;)

	rm $key $cert
done
