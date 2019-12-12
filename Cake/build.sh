#!/usr/bin/env bash
# Define varibles
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
DOTNET_VERSION="2.2.109"
CAKE_VERSION="0.33.0"
TOOLS_DIR=$SCRIPT_DIR/tools
CAKE_EXE=$TOOLS_DIR/dotnet-cake
CAKE_PATH=$TOOLS_DIR/.store/cake.tool/$CAKE_VERSION

# Make sure the tools folder exist.
if [ ! -d "$TOOLS_DIR" ]; then
  mkdir "$TOOLS_DIR"
fi

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
export DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

DOTNET_INSTALLED_VERSION=$(dotnet --version 2>&1)

if [ "$DOTNET_VERSION" != "$DOTNET_INSTALLED_VERSION" ]; then
    INSTALLATION_PATH="$HOME/.dotnet"
	echo "Installing .NET CLI. Version:" $DOTNET_VERSION "Installation Path:" $INSTALLATION_PATH
	
    if [ ! -d $INSTALLATION_PATH ]; then
      mkdir $INSTALLATION_PATH
	  echo "Directory is created" $INSTALLATION_PATH
    fi
	
    curl -Lsfo "$SCRIPT_DIR/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
    bash "$SCRIPT_DIR/dotnet-install.sh" --version $DOTNET_VERSION --install-dir $INSTALLATION_PATH --no-path
    export PATH=$INSTALLATION_PATH:$PATH
    export DOTNET_ROOT=$INSTALLATION_PATH
fi

###########################################################################
# INSTALL CAKE
###########################################################################

CAKE_INSTALLED_VERSION=$(dotnet-cake --version 2>&1)

if [ "$CAKE_VERSION" != "$CAKE_INSTALLED_VERSION" ]; then
    if [ ! -f "$CAKE_EXE" ] || [ ! -d "$CAKE_PATH" ]; then
        if [ -f "$CAKE_EXE" ]; then
            dotnet tool uninstall --tool-path $TOOLS_DIR Cake.Tool
        fi

        echo "Installing Cake $CAKE_VERSION..."
        dotnet tool install --tool-path $TOOLS_DIR --version $CAKE_VERSION Cake.Tool
        if [ $? -ne 0 ]; then
            echo "An error occured while installing Cake."
            exit 1
        fi
    fi

    # Make sure that Cake has been installed.
    if [ ! -f "$CAKE_EXE" ]; then
        echo "Could not find Cake.exe at '$CAKE_EXE'."
        exit 1
    fi
else
    CAKE_EXE="dotnet-cake"
fi

###########################################################################
# RUN BUILD SCRIPT
###########################################################################
# Define default arguments.
# Define default arguments.
SCRIPT="build.cake"
TARGET="Default"
SCRIPT_ARGUMENTS=()

# Parse arguments.
for i in "$@"; do
    case $1 in
        -s|--script) SCRIPT="$2"; shift ;;
        -t|--target) TARGET="$2"; shift ;;
        --) shift; SCRIPT_ARGUMENTS+=("$@"); break ;;
        *) SCRIPT_ARGUMENTS+=("$1") ;;
    esac
    shift
done


# Start Cake
(exec "$CAKE_EXE" $SCRIPT "--bootstrap") & (exec "$CAKE_EXE" $SCRIPT ${SCRIPT_ARGUMENTS[*]})
