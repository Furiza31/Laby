#!/bin/sh

set -e

# Start the application
dotnet run --no-launch-profile --project Laby.Client.Console -- "$@"