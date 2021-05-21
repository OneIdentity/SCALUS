#!/bin/sh

here=`pwd`
if [ ! -f ${here}/scalus ]; then 
     here=`dirname $0`
     if  [ ! -f ${here}/scalus ]; then 
        "Please run this script from the unzipped scalus directory" 
         exit 1
     fi
fi
chown -R root:root  $here
chmod 0755 `find $here -type d`
chmod 0644 `find $here -type f`
chmod 0755 $here/scalus

if [ -h /usr/bin/scalus ]; then 
    rm -f /usr/bin/scalus
fi
ln -s  ${here}/scalus /usr/bin/scalus
cat ${here}/readme.txt


