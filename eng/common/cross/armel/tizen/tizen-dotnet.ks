lang en_US.UTF-8
keyboard us
timezone --utc Asia/Seoul

part / --fstype="ext4" --size=3500 --ondisk=mmcblk0 --label rootfs --fsoptions=defaults,noatime

rootpw tizen
desktop --autologinuser=root
user --name root  --groups audio,video --password 'tizen'

repo --name=standard  --baseurl=http://download.tizen.org/releases/milestone/tizen/unified/latest/repos/standard/packages/ --ssl_verify=no
repo --name=base      --baseurl=http://download.tizen.org/releases/milestone/tizen/base/latest/repos/standard/packages/ --ssl_verify=no

%packages
tar
gzip

sed
grep
gawk
perl

binutils
findutils
util-linux
lttng-ust
userspace-rcu
procps-ng
tzdata
ca-certificates


### Core FX
libicu
libunwind
iputils
zlib
krb5
libcurl
libopenssl

%end

%post

### Update /tmp privilege
chmod 777 /tmp
####################################

%end
