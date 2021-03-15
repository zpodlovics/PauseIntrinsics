#!/bin/bash

SCRIPT=$(readlink -f "$0")
SCRIPTPATH=$(dirname "$SCRIPT")
BASEPATH="${SCRIPTPATH}"

mkdir -p ${BASEPATH}/artifacts
for i in `ls ${BASEPATH}/tests`; do dotnet publish -c Release -r linux-x64 ${BASEPATH}/tests/$i -o ${BASEPATH}/artifacts/$i; done
