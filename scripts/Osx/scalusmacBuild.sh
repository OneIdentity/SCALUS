#!/bin/bash

scalusmacdir=""

PARAMS=""
while(( "$#" )); do
    case "$1" in
       --scalusmacdir)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
            echo "Error : missing scalusmacdir"
            shift
            exit 1
     fi
         scalusmacdir="$2"
     shift 2
    ;;
      *)
        shift
        ;;
    esac
done

if [ -z "${scalusmacdir}" ]; then 
    echo "missing scalusmacdir"
    exit 1
fi

cd ${scalusmacdir}
swift build --configuration release

echo "Finished building scalusmac"