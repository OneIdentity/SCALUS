#!/bin/bash

version="1.0"
runtime="osx-x64"

inpath=""
outpath=""

appname="scalus"

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
  
pkgname="${appname}-${version}_${runtime}.pkg"
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

function resetInfo()
{

    filename="${tmpdir}/${appname}.app/Contents/Info.plist"
    if [ ! -f ${filename} ]; then 
	echo "ERROR - missing file:${filename}"
        exit 1
    fi
    /bin/bash -c "defaults write $filename CFBundleURLTypes  -array \
'
<array>
	<dict>
		<key>CFBundleURLName</key>
			<string>com.oneidentity.${appname}.macos</string>
		<key>CFBundleURLSchemes</key>
			<array>
			<string>rdp</string>
			<string>ssh</string>
			<string>telnet</string>
		</array>
	</dict>
</array> '"


/bin/bash -c "defaults write $filename CFBundleVersion  -string \"${version}\""
/bin/bash -c "defaults write $filename CFBundleShortVersion  -string \"${version}\""
/bin/bash -c "defaults write $filename CFBundleName  -string 'Scalus'"
/bin/bash -c "defaults write $filename CFBundleDisplayName  -string 'Session URL Launcher Utility'"
/bin/bash -c "defaults write $filename CFBundleIdentifier  -string  'com.oneidentity.${appname}.macos'"

    chmod a+r $filename
}

function make_app()
{
	if [ -d ${tmpdir} ]; then 
        	rm -rf ${tmpdir}
	fi
        mkdir -p ${tmpdir}

        cat ${inpath}/applet | awk -v appname="com.oneidentity.${appname}.macos" '
{
        str=sprintf("kMDItemCFBundleIdentifier=%s", appname);
        sub(/kMDItemCFBundleIdentifier=\S+/, str);
	print $0
}' > ${inpath}/applet.tmp
if [ $? -eq 0 ]; then 
	mv ${inpath}/applet.tmp ${inpath}/applet
fi

        osacompile -o ${tmpdir}/${appname}.app ${inpath}/applet
        resetInfo
}


function build_package()
{
        rm -f ${pkgfile}
        productbuild --component  ${tmpdir}/${appname}.app "Applications" ${pkgfile}
	expdir="${outpath}/tmp2"
	rm -rf ${expdir}

	pkgutil --expand $pkgfile ${expdir}
	cat ${expdir}/Distribution | awk -v pkg="${pkgname}" -v appname="${appname}" '
{
        str=""
        sub(/customLocation=\"[^\"]+\"/, str);
	if ($0 ~ /<\/installer-gui-script>/)
        {
                print "  <domains enable_anywhere=\"false\" enable_currentUserHome=\"true\" enable-localSystem=\"false\">"
                print "  </domains>"
        }

        print $0;
}' > ${expdir}/Distribution2
	if [ $? -ne 0 ]; then
		echo "*** Failed to change file"
		exit 1
	fi
	mv ${expdir}/Distribution2 ${expdir}/Distribution
	
	pkgutil --flatten ${expdir} ${pkgfile}
        rm -rf ${tmpdir}/${appname}.app
        rm -rf ${expdir}
}
make_app
build_package

echo "Finished generating ${pkgfile}"
