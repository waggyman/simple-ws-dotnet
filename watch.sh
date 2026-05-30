#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
export DOTNET_USE_POLLING_FILE_WATCHER=true
exec dotnet watch run --launch-profile http "$@"
