#!/usr/bin/env bash

curl -sSL http://nginx.org/download/nginx-1.14.2.tar.gz | tar zxfv - -C /tmp && cd /tmp/nginx-1.14.2/
./configure --prefix=$HOME/nginxinstall --with-http_ssl_module
make
make install
echo "##vso[task.prependpath]$HOME/nginxinstall/sbin"