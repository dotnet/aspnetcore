#!/usr/bin/env bash

set -euo pipefail

scriptroot="$( cd -P "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
reporoot="$(dirname "$(dirname "$scriptroot")")"
nginxinstall="$reporoot/.tools/nginx"

curl -sSL http://nginx.org/download/nginx-1.26.3.tar.gz --retry 5 | tar zxfv - -C /tmp && cd /tmp/nginx-1.26.3/
./configure --prefix=$nginxinstall --with-http_ssl_module --without-http_rewrite_module
make
make install
echo "##vso[task.prependpath]$nginxinstall/sbin"
