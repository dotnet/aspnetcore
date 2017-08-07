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

apt-get update

echo "Updating dhclient.conf..."
echo 'supersede domain-name "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf
echo 'supersede domain-search "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf
echo 'supersede search "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf

echo "Restarting dhclient..."
dhclient -r
dhclient

echo "Installing Git..."
apt-get install -y git

# Sometimes git pull stalls, so this could fix it
git config --global http.postBuffer 2M

echo "Installing libunwind8..."
apt-get install -y libunwind8

echo "Installing Java..."
apt-get install -y default-jre-headless unzip

echo "Installing Node.js..."
curl -sL https://deb.nodesource.com/setup_6.x | sudo -E bash -
apt-get install -y nodejs

echo "Installing TypeScript globally..."
npm install -g typescript

echo "Installing Nginx..."
apt-get install -y nginx
update-rc.d nginx defaults

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
sed -i "s|^name=.*|name=$AGENTNAME|" buildAgent.properties
sed -i "s|^serverUrl=.*|serverUrl=$SERVERURL|" buildAgent.properties

# Use the local SSD (/mnt) for the work and temp dirs.
# * It should be as fast or faster than the OS disk (which is backed by blob storage).
# * It reduces load and storage on the OS disk.
# * The data on the local SSD may lost at any time (typically after a reboot), but this
#   should be fine for the work and temp dirs.
sed -i "s|^workDir=.*|workDir=/mnt/work|" buildAgent.properties
sed -i "s|^tempDir=.*|tempDir=/mnt/temp|" buildAgent.properties

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
update-rc.d agentStartStop defaults

echo "Starting the build agent..."
~/TeamCity/bin/agent.sh start
