#!/bin/sh

top=`pwd`

filename="`pwd`/scalus.app/Contents/Info.plist"


/bin/sh -c "defaults write $filename CFBundleURLTypes  -array \
'
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>com.oneidentity.scalus.macos</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>rdp</string>
            <string>ssh</string>
            <string>telnet</string>
        </array>
    </dict>
</array>'"


/bin/sh -c "defaults write $filename CFBundleName  -string 'Scalus'"
/bin/sh -c "defaults write $filename CFBundleDisplayName  -string 'Session URL Launcher Utility'"
/bin/sh -c "defaults write $filename CFBundleIdentifier  -string  'com.oneidentity.scalus.macos'"

/bin/sh -c "defaults write $filename CFBundleDocumentTypes -array \
'
<array>
  <dict>
    <key>CFBundleTypeExtensions</key>
    <array>
    </array>
    <key>CFBundleTypeIconFile</key>
    <string></string>
    <key>CFBundleTypeMIMETypes</key>
    <array>
      <string>application/x-rdp</string>
    </array>

    <key>CFBundleTypeName</key>
    <string>com.oneidentity.scalus.macos</string>
    <key>CFBundleTypeRole</key>
    <string>Viewer</string>
  </dict>
</array>
'"
