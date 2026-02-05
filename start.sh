#!/bin/sh

set -e

# Start the application
dotnet run -p:BuildInParallel=false --no-launch-profile --project Laby.Client.Console -- "$@"
