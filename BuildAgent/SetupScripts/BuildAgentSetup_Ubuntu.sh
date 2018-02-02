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

sudo apt-get update

echo "Updating dhclient.conf..."
echo 'supersede domain-name "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf
echo 'supersede domain-search "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf
echo 'supersede search "redmond.corp.microsoft.com";' >> /etc/dhcp/dhclient.conf

echo "Restarting dhclient..."
sudo dhclient -r
sudo dhclient

echo "Installing Git..."
sudo apt-get install -y git

# Sometimes git pull stalls, so this could fix it
git config --global http.postBuffer 2M

echo "Installing .NET Core Prereqs..."
sudo apt-get install -y libunwind8 liblttng-ust0 libcurl3 libssl1.0.0 libuuid1 libkrb5-3 zlib1g libicu55

echo "Installing Java..."
sudo apt-get install -y default-jre-headless unzip

echo "Installing Node.js..."
curl -sL https://deb.nodesource.com/setup_8.x | sudo -E bash -
sudo apt-get install -y nodejs

echo "Installing TypeScript globally..."
sudo npm install -g typescript

echo "Installing Nginx..."
sudo apt-get install -y nginx
sudo update-rc.d nginx defaults

echo "Installing Docker..."
export CHANNEL=stable
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $SUDO_USER

echo "Downloading build agent from $SERVERURL and updating the properties..."
mkdir ~/BuildAgent
cd ~/BuildAgent
wget $SERVERURL/update/buildAgent.zip
unzip buildAgent.zip
cd bin
chmod +x agent.sh
cd ~/BuildAgent/conf
cp buildAgent.dist.properties buildAgent.properties

# Set the build agent name and CI server urls
sed -i "s|^name=.*|name=$AGENTNAME|" buildAgent.properties
sed -i "s|^serverUrl=.*|serverUrl=$SERVERURL|" buildAgent.properties

# Use the local SSD (/mnt) for the work, temp, and system dirs.
# * It should be as fast or faster than the OS disk (which is backed by blob storage).
# * It reduces load and storage on the OS disk.
# * The data on the local SSD may lost at any time (typically after a reboot), but this
#   should be fine for the work, temp, and system dirs.
sed -i "s|^workDir=.*|workDir=/mnt/work|" buildAgent.properties
sed -i "s|^tempDir=.*|tempDir=/mnt/temp|" buildAgent.properties
sed -i "s|^systemDir=.*|systemDir=/mnt/system|" buildAgent.properties

echo >> buildAgent.properties # append a new line
echo "system.aspnet.os.name=$ASPNETOSNAME" >> buildAgent.properties

cd ~/BuildAgent

sudo cat <<EOF >> buildAgent
#!/bin/sh
### BEGIN INIT INFO
# Provides:          TeamCity Build Agent
# Required-Start:    $remote_fs $syslog
# Required-Stop:     $remote_fs $syslog
# Default-Start:     2 3 4 5
# Default-Stop:      0 1 6
# Short-Description: Start build agent daemon at boot time
# Description:       Enable service provided by daemon.
### END INIT INFO
#Provide the correct user name:
USER="aspnetagent"

case "$1" in
start)
 # Grant all users write access to /mnt, since TeamCity uses /mnt for temp storage
 chmod a+w /mnt
 su - $USER -c "cd BuildAgent/bin ; ./agent.sh start"
;;
stop)
 su - $USER -c "cd BuildAgent/bin ; ./agent.sh stop"
;;
*)
  echo "usage start/stop"
  exit 1
 ;;

esac

exit 0
EOF

sudo chmod +x buildAgent
sudo mv buildAgent /etc/init.d/
sudo update-rc.d buildAgent defaults

echo "Logout and login to finish docker config, then run 'sudo service buildAgent start' to start agent"
