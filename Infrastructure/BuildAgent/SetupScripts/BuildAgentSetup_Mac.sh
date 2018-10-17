#!/usr/bin/env bash
trap 'exit' ERR # exit as soon as a command fails

AGENTNAME=$(hostname)
SERVERURL="http://aspnetci/"
ASPNETOSNAME="osx"

while [[ $# > 0 ]]; do
    case $1 in
        -a)
            shift
            AGENTNAME=$1
            ;;
        -url)
            shift
            SERVERURL=${1%/} # trim the final '/' in the string
            ;;
        -osname)
            shift
            ASPNETOSNAME=$1
            ;;
        -help|*)
            echo "Usage: [-a <agent-name>] [-url <agent-url>] [-osname <aspnet-os-name>]"
            echo "       -a <agent-name>            The name of the build agent"
            echo "       -url <agent-url>           The TeamCity server url"
            echo "       -osname <aspnet-os-name>   The value for build agent property 'aspnet.os.name'"
            echo ""
            echo "Examples:"
            echo "-a 'aspnetci-b01' -url 'http://aspnetci/' -osname 'osx'"
            exit 1
    esac
    shift
done

ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"

brew update

echo "Installing Git..."
brew install git

# Sometimes git pull stalls, so this could fix it
git config --global http.postBuffer 2M

echo "Installing Mono..."
brew install mono

echo "Installing openssl..."
brew install openssl
brew link --force openssl

echo "Installing Java..."
brew install caskroom/versions/java7

echo "Installing Node.js..."
brew install node

echo "Installing node-pre-gyp globally..."
npm install -g node-pre-gyp

echo "Installing Bower globally..."
npm install -g bower
echo '{ "allow_root": true }' > ~/.bowerrc # workaround for bower install errors when using with sudo

echo "Installing Grunt globally..."
npm install -g grunt

echo "Installing Gulp globally..."
npm install -g gulp

echo "Installing TypeScript globally..."
npm install -g typescript
npm install -g tsd

echo "Installing Nginx..."
brew install nginx

echo "Starting Nginx..."
brew services start nginx

echo "Installing wget..."
brew install wget

echo "Installing gnu-sed..."
brew install gnu-sed --with-default-names

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

chmod \+x ~/TeamCity/launcher/bin/mac.launchd.sh
sudo sh ~/TeamCity/bin/mac.launchd.sh load
tail -f ~/TeamCity/logs/teamcity-agent.log
sh ~/TeamCity/bin/mac.launchd.sh unload
sudo cp ~/TeamCity/bin/jetbrains.teamcity.BuildAgent.plist $HOME/Library/LaunchAgents/

echo "Starting the build agent..."
~/TeamCity/bin/agent.sh start