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

if [[ `lsb_release -rs` == "14.04" ]]
then
    # Increase limit on 14.04 from default of 128 to 1024, to match default on 16.04 and 18.04
    # With defaut limit of 128, some of our tests fail with:
    #
    # System.IO.IOException : The configured user limit (128) on the number of inotify instances has been reached.
    #    at System.IO.FileSystemWatcher.StartRaisingEvents()
    echo fs.inotify.max_user_instances=1024 | sudo tee -a /etc/sysctl.conf && sudo sysctl -p
fi

echo "Installing curl and unzip..."
sudo apt-get install -y curl unzip

if [[ `lsb_release -rs` == "14.04" ]] || [[ `lsb_release -rs` == "16.04" ]]
then
    echo "Updating dhclient.conf..."
    echo 'supersede domain-name "redmond.corp.microsoft.com";' | sudo tee --append /etc/dhcp/dhclient.conf
    echo 'supersede domain-search "redmond.corp.microsoft.com";' | sudo tee --append /etc/dhcp/dhclient.conf
    echo 'supersede search "redmond.corp.microsoft.com";' | sudo tee --append /etc/dhcp/dhclient.conf

    echo "Restarting dhclient..."
    sudo dhclient -r
    sudo dhclient
elif [[ `lsb_release -rs` == "18.04" ]]
then
    echo "Updating /etc/netplan/50-cloud-init.yaml..."
    echo '            nameservers:' | sudo tee --append /etc/netplan/50-cloud-init.yaml
    echo '                search: [redmond.corp.microsoft.com]' | sudo tee --append /etc/netplan/50-cloud-init.yaml

    echo "Applying netplan changes..."
    sudo netplan apply
else
    echo "Unknown version: `lsb_release -rs`"
    exit 1
fi

echo "Installing Git..."
sudo apt-get install -y git

# Sometimes git pull stalls, so this could fix it
git config --global http.postBuffer 2M

# https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites
echo "Installing .NET Core Prereqs..."

sudo apt-get install -y libunwind8 liblttng-ust0 libcurl3 libssl1.0.0 libuuid1 libkrb5-3 zlib1g

if [[ `lsb_release -rs` == "14.04" ]]
then
    sudo apt-get install -y libicu52
elif [[ `lsb_release -rs` == "16.04" ]]
then
    sudo apt-get install -y libicu55
elif [[ `lsb_release -rs` == "18.04" ]]
then
    sudo apt-get install -y libicu60
else
    echo "Unknown version: `lsb_release -rs`"
    exit 1
fi

echo "Installing Java..."
if [[ `lsb_release -rs` == "14.04" ]]
then
    sudo apt-get install -y openjdk-7-jre-headless
elif [[ `lsb_release -rs` == "16.04" ]] || [[ `lsb_release -rs` == "18.04" ]]
then
    # On Ubuntu 18.04, default-jre-headless maps to openjdk-10, which is incompatible with TeamCity 2017.
    # Safest to install openjdk-8 on both Ubuntu 16.04 and 18.04
    sudo apt-get install -y openjdk-8-jre-headless
else
    echo "Unknown version: `lsb_release -rs`"
    exit 1
fi

echo "Installing Node.js..."
curl -sL https://deb.nodesource.com/setup_8.x | sudo -E bash -
sudo apt-get install -y nodejs

echo "Installing TypeScript globally..."
sudo npm install -g typescript

echo "Installing Nginx..."
sudo apt-get install -y nginx
sudo update-rc.d nginx defaults

echo "Installing components required for Autobahn test suite..."
source Components/ensure-autobahn.sh

echo "Installing Docker..."
export CHANNEL=stable
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER

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

# Without this setting, git commands will fail with the following error:
# 
# There was a problem while connecting to github.com:22
# fatal: Could not read from remote repository.
# Please make sure you have the correct access rights and the repository exists.
echo >> buildAgent.properties # append a new line
echo "teamcity.git.use.native.ssh=true" >> buildAgent.properties

echo >> ~/.profile
echo '# Add /usr/sbin to path if not already present.  Required for TeamCity to execute /usr/sbin/nginx.' >> ~/.profile
echo '[[ ":$PATH:" != *":/usr/sbin:"* ]] && PATH="/usr/sbin:${PATH}"' >> ~/.profile

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

case "\$1" in
start)
 # Grant all users write access to /mnt, since TeamCity uses /mnt for temp storage
 chmod a+w /mnt
 su - \$USER -c "cd BuildAgent/bin ; ./agent.sh start"
;;
stop)
 su - \$USER -c "cd BuildAgent/bin ; ./agent.sh stop"
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
