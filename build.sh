#!/bin/bash

SCRIPT=$(readlink -f "$0")
SCRIPTPATH=$(dirname "$SCRIPT")
BASEPATH="${SCRIPTPATH}"

dotnet build -c Release $@
