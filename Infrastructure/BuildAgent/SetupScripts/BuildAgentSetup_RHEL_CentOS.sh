#!/usr/bin/env bash
trap 'exit' ERR # exit as soon as a command fails

# NOTE: This script must be run with an account having 'sudo' privileges

if [ $# -ne 3 ]; then
    echo "Invalid number of arguments specified."
    echo "Usage: <script-name> <agent-name> <agent-url> <aspnet-os-name>"
    echo "Examples:"
    echo "<script-name> 'aspnetci-b01' 'http://aspnetci/' 'ubuntu'"
    exit 1
fi

AGENTNAME=$1
SERVERURL=${2%/} # trim the final '/' in the string
ASPNETOSNAME=$3

yum check-update

echo "Updating dhclient.conf..."
echo 'supersede domain-name "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf
echo 'supersede domain-search "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf
echo 'supersede search "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf

echo "Restarting dhclient..."
dhclient -r
dhclient

echo "Installing Git..."
yum install -y git

# Sometimes git pull stalls, so this could fix it
git config --global http.postBuffer 2M

yum install -y epel-release

echo "Installing Mono 4.2.3..."
yum install yum-utils
rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
yum-config-manager --add-repo http://download.mono-project.com/repo/centos/
yum check-update
yum install -y mono-complete

echo "Installing libunwind..."
yum install -y libunwind

echo "Installing Java..."
yum install -y java-1.7.0-openjdk 

echo "Installing Node.js..."
curl --silent --location https://rpm.nodesource.com/setup_4.x | bash -
yum -y install nodejs
ln -s /usr/bin/nodejs /usr/bin/node

echo "Installing NPM..."
yum -y install npm

echo "Installing Bower globally..."
npm install -g bower
echo '{ "allow_root": true }' > /root/.bowerrc # workaround for bower install errors when using with sudo

echo "Installing Grunt globally..."
npm install -g grunt

echo "Installing Gulp globally..."
npm install -g gulp

echo "Installing TypeScript globally..."
npm install -g typescript

echo "Installing Nginx..."
yum install -y nginx
systemctl start nginx # start nginx
systemctl enable nginx # enable Nginx to start when your system boots

yum install -y unzip

echo "Downloading build agent from $SERVERURL and updating the properties..."
mkdir ~/TeamCity
cd ~/TeamCity
wget $SERVERURL/update/buildAgent.zip
unzip buildAgent.zip
cd bin
chmod +x agent.sh
cd ~/TeamCity/conf
cp buildAgent.dist.properties buildAgent.properties

# Set the build agent name and CI server urls
sed -i "s/name=.*/name=$AGENTNAME/" buildAgent.properties
sed -i "s#serverUrl=.*#serverUrl=$SERVERURL#g" buildAgent.properties

echo >> buildAgent.properties # append a new line
echo "system.aspnet.os.name=$ASPNETOSNAME" >> buildAgent.properties

cd ~/TeamCity

cat <<EOF >> agentStartStop
#!/usr/bin/env bash
### BEGIN INIT INFO
# Provides:          TeamCity build agent
# Required-Start:    $remote_fs $syslog
# Required-Stop:     $remote_fs $syslog
# Default-Start:     2 3 4 5
# Default-Stop:      0 1 6
# Short-Description: Start daemon at boot time
# Description:       Enable service provided by daemon.
### END INIT INFO
case "\$1" in
start)
 sudo ~/TeamCity/bin/agent.sh start
;;
stop)
 sudo ~/TeamCity/bin/agent.sh stop
;;
*)
  echo "usage start/stop"
  exit 1
 ;;
 
esac
 
exit 0
EOF

chmod +x agentStartStop
cp agentStartStop /etc/init.d/
sudo chkconfig agentStartStop on

~/TeamCity/bin/agent.sh start
