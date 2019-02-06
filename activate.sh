#
# This file must be used by invoking "source activate.sh" from the command line.
# You cannot run it directly.
# To exit from the environment this creates, execute the 'deactivate' function.

_MAGENTA="\033[0;95m"
_YELLOW="\033[0;33m"
_RESET="\033[0m"

deactivate () {

    # reset old environment variables
    if [ ! -z "${_OLD_PATH:-}" ] ; then
        export PATH="$_OLD_PATH"
        unset _OLD_PATH
    fi

    if [ ! -z "${_OLD_PS1:-}" ] ; then
        export PS1="$_OLD_PS1"
        unset _OLD_PS1
    fi

    # This should detect bash and zsh, which have a hash command that must
    # be called to get it to forget past commands.  Without forgetting
    # past commands the $PATH changes we made may not be respected
    if [ -n "${BASH:-}" ] || [ -n "${ZSH_VERSION:-}" ] ; then
        hash -r 2>/dev/null
    fi

    unset DOTNET_ROOT
    unset DOTNET_MULTILEVEL_LOOKUP
    if [ ! "${1:-}" = "init" ] ; then
        # Remove the deactivate function
        unset -f deactivate
    fi
}

# Cleanup the environment
deactivate init

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
_OLD_PATH="$PATH"
# Tell dotnet where to find itself
export DOTNET_ROOT="$DIR/.dotnet"
# Tell dotnet not to look beyond the DOTNET_ROOT folder for more dotnet things
export DOTNET_MULTILEVEL_LOOKUP=0
# Put dotnet first on PATH
export PATH="$DOTNET_ROOT:$PATH"

# Set the shell prompt
if [ -z "${DISABLE_CUSTOM_PROMPT:-}" ] ; then
    _OLD_PS1="$PS1"
    export PS1="(`basename \"$DIR\"`) $PS1"
fi

# This should detect bash and zsh, which have a hash command that must
# be called to get it to forget past commands.  Without forgetting
# past commands the $PATH changes we made may not be respected
if [ -n "${BASH:-}" ] || [ -n "${ZSH_VERSION:-}" ] ; then
    hash -r 2>/dev/null
fi

echo "${_MAGENTA}Enabled the .NET Core environment. Execute 'deactivate' to exit.${_RESET}"

if [ ! -f "$DOTNET_ROOT/dotnet" ]; then
    echo "${_YELLOW}.NET Core has not been installed yet. Run $DIR/restore.sh to install it.${_RESET}"
else
    echo "dotnet = $DOTNET_ROOT/dotnet"
fi
