FROM ubuntu:14.04

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list

ENV DOTNET_INSTALL_DIR /opt/dotnet

RUN apt-get -qq update
RUN apt-get -qqy install \
	curl \
	unzip \
	libunwind8 \
	libicu52 \
	libcurl3-gnutls \
	libssl-dev \
	gettext

ENV CHANNEL beta
ENV VERSION 1.0.0.001496
RUN curl -sSL https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.sh | /bin/bash

RUN apt-get -qqy install \
	autoconf \
	automake \
	build-essential \
	libtool

RUN LIBUV_VERSION=1.4.2 \
	&& curl -sSL https://github.com/libuv/libuv/archive/v${LIBUV_VERSION}.tar.gz | tar zxfv - -C /usr/local/src \
	&& cd /usr/local/src/libuv-$LIBUV_VERSION \
	&& sh autogen.sh && ./configure && make && make install \
	&& rm -rf /usr/local/src/libuv-$LIBUV_VERSION \
	&& ldconfig

ENV DOTNET_HOME $DOTNET_INSTALLDIR/share/dotnet/cli
ENV DOTNET_REFERENCE_ASSEMBLIES_PATH /usr/lib/mono/xbuild-frameworks
ENV PATH $DOTNET_INSTALL_DIR/bin:$PATH

COPY . /app
WORKDIR /app
RUN ["dotnet", "restore"]

EXPOSE 5000
ENTRYPOINT ["dotnet", "run", "--", "--server.urls", "http://0.0.0.0:5000"]
