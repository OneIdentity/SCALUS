#!/bin/bash


version="1.0"
runtime="osx-x64"

#path must contain scalus.app
inpath=""
outpath=""


PARAMS=""
while(( "$#" )); do
    case "$1" in
       --version)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
         	echo "Error : missing version"
         	shift
         	exit 1
	 fi
         version="$2"
	 shift 2
	;;
       --runtime)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
         	echo "Error : missing runtime"
         	shift
         	exit 1
	 fi
         runtime="$2"
	 shift 2
	;;
       --inpath)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
         	echo "Error : missing inpath"
         	shift
         	exit 1
	 fi
         inpath="$2"
	 shift 2
	;;
       --outpath)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
         	echo "Error : missing outpath"
         	shift
         	exit 1
	 fi
         outpath="$2"
	 shift 2
	;;
      *)
        shift
        ;;
    esac
done
  
pkgname="scalus-1.0_osx-x64.pkg"
pkgfile="${outpath}/${pkgname}"

if [ -z "${inpath}" ]; then 
	echo "Missing inpath"
	exit 1
fi
if [ -z "${outpath}" ]; then 
	echo "Missing outpath"
	exit 1
fi
if [ -d "${inpath}/applet" ]; then 
	echo "inpath must contain applet file"
	exit 1
fi

echo "Building from ${inpath}/applet"
echo "Building ${pkgfile}"


tmpdir="${outpath}/tmp"
mkdir -p "${tmpdir}"



function resetInfo()
{
    filename="${tmpdir}/scalus.app/Contents/Info.plist"
    if [ ! -f ${filename} ]; then 
	echo "ERROR - missing file:${filename}"
        exit 1
    fi
    /bin/bash -c "defaults write $filename CFBundleURLTypes  -array \
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
</array> '"


/bin/bash -c "defaults write $filename CFBundleName  -string 'Scalus'"
/bin/bash -c "defaults write $filename CFBundleDisplayName  -string 'Session URL Launcher Utility'"
/bin/bash -c "defaults write $filename CFBundleIdentifier  -string  'com.oneidentity.scalus.macos'"
/bin/bash -c "defaults write $filename CFBundleDocumentTypes -array \
'
<array>
	<dict>
		<key>CFBundleTypeExtensions</key>
		<array></array>
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
</array> '"

}
osacompile -o ${tmpdir}/scalus.app ${inpath}/applet

resetInfo


productbuild --component  ${tmpdir}/scalus.app /Applications  ${pkgfile}
#--sign 3rd Party Mac Developer Installer

echo "Finished generating ${pkgfile}"
