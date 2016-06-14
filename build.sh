#!/usr/bin/env bash
repoFolder="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $repoFolder

koreBuildZip="https://github.com/aspnet/KoreBuild/archive/release.zip"
if [ ! -z $KOREBUILD_ZIP ]; then
    koreBuildZip=$KOREBUILD_ZIP
fi

buildFolder=".build"
buildFile="$buildFolder/KoreBuild.sh"

if test ! -d $buildFolder; then
    echo "Downloading KoreBuild from $koreBuildZip"
    
    tempFolder="/tmp/KoreBuild-$(uuidgen)"    
    mkdir $tempFolder
    
    localZipFile="$tempFolder/korebuild.zip"
    
    retries=6
    until (wget -O $localZipFile $koreBuildZip 2>/dev/null || curl -o $localZipFile --location $koreBuildZip 2>/dev/null)
    do
        echo "Failed to download '$koreBuildZip'"
        if [ "$retries" -le 0 ]; then
            exit 1
        fi
        retries=$((retries - 1))
        echo "Waiting 10 seconds before retrying. Retries left: $retries"
        sleep 10s
    done
    
    unzip -q -d $tempFolder $localZipFile
  
    mkdir $buildFolder
    cp -r $tempFolder/**/build/** $buildFolder
    
    chmod +x $buildFile
    
    # Cleanup
    if test ! -d $tempFolder; then
        rm -rf $tempFolder  
    fi
fi

$buildFile -r $repoFolder "$@"