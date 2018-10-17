#!/usr/bin/env bash

set +e

if ! type -p python >/dev/null 2>&1; then
	echo "Requires python!" 1>&2
	exit 1
fi

sudo apt-get install -y python-dev python-virtualenv gcc

mkdir -p ~/virtualenvs

if [ ! -d ~/virtualenvs/autobahntestsuite ]; then
	virtualenv ~/virtualenvs/autobahntestsuite
fi

# May be required for Ubuntu 14.04, verify when creating new agent
# if [[ `lsb_release -rs` == "14.04" ]]
# then
# 	~/virtualenvs/autobahntestsuite/bin/pip install -U setuptools
# fi

~/virtualenvs/autobahntestsuite/bin/pip install autobahntestsuite

if [ -e /usr/local/bin/wstest ]; then
	sudo rm /usr/local/bin/wstest
fi
sudo ln -s ~/virtualenvs/autobahntestsuite/bin/wstest /usr/local/bin/wstest
