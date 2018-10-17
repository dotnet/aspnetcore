#/usr/bin/env bash
if [[ "$(uname)" = "Darwin" ]]; then
    # Java 1.8 is already installed on the mac agents, but verify it
    if ! type -p javac >/dev/null; then
        echo "Java is missing! It must be manually installed on macOS machines." 1>&2
        exit 1
    fi

    brew install gpg
else
    # Right now, it appears all the Ubuntu agents have a JDK and GPG, so I didn't write script to install them.
    if ! type -p javac >/dev/null; then
        echo "Java is missing! It is normally already installed on the agents, so this script doesn't install it." 1>&2
    fi

    if ! type -p gpg >/dev/null; then
        echo "GPG is missing! It is normally already installed on the agents, so this script doesn't install it." 1>&2
    fi
fi