#!/bin/sh

top=`pwd`

filename="`pwd`/scalus.app/Contents/CodeSignInfo.plist"


/bin/sh -c "defaults write $filename CFBundleName  -string 'codesign'"