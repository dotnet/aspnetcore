#!/usr/bin/env bash

set +e

if ! type -p python >/dev/null 2>&1; then
	echo "Requires python!" 1>&2
	exit 1
fi

python -m ensurepip
pip install virtualenv

mkdir -p ~/virtualenvs

if [ ! -d ~/virtualenvs/autobahntestsuite ]; then
	virtualenv ~/virtualenvs/autobahntestsuite
fi

~/virtualenvs/autobahntestsuite/bin/pip install autobahntestsuite

if [ -e /usr/local/bin/wstest ]; then
	rm /usr/local/bin/wstest
fi
ln -s ~/virtualenvs/autobahntestsuite/bin/wstest /usr/local/bin/wstest
