#!/bin/bash
#curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -channel 6.0
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -channel 7.0 -Installdir $HOME/.cli -Nopath
#curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -channel 8.0


remove_from_path() {
  local partial="$1"
  echo $PATH | tr ':' '\n' | grep -v "$partial" | paste -sd ':' -
}

# Usage example: Remove paths containing '.dotnet'
NEWPATH=$(remove_from_path '.dotnet')

echo "DOTNET_ROOT=$HOME/.cli" > DOTNETROOT.txt
echo "PATH=$NEWPATH:$HOME/.cli/tools" > DOTNETTOOLSPATH.txt

