#!/bin/bash

SCRIPTPATH="$(dirname "$0")"
LD_LIBRARY_PATH=$SCRIPTPATH/:$SCRIPTPATH/OpenNI2/Drivers:$LD_LIBRARY_PATH $SCRIPTPATH/sandbox.x86*64 "$@"