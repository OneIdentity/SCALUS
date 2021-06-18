#!/bin/bash

version="1.0"
runtime="osx-x64"

infile=""
outpath=""

appname="scalus"
publishdir=""

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
       --infile)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
         	echo "Error : missing infile"
         	shift
         	exit 1
	 fi
         infile="$2"
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
       --publishdir)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
         	echo "Error : missing publishdir"
         	shift
         	exit 1
	 fi
         publishdir="$2"
	 shift 2
	;;
      *)
        shift
        ;;
    esac
done
  
pkgname="${appname}-${version}_${runtime}.pkg"
pkgfile="${outpath}/${pkgname}"

pkgtar="${appname}-${version}_${runtime}.tar.gz"
pkgtarfile="${outpath}/${pkgtar}"


if [ -z "${infile}" ]; then 
	echo "Missing infile"
	exit 1
fi
if [ -z "${outpath}" ]; then 
	echo "Missing outpath"
	exit 1
fi
if [ ! -f "${infile}" ]; then 
	echo "infile must contain full path of applet"
	exit 1
fi
if [ -z "${publishdir}" ]; then 
	echo "missing publishdir"
	exit 1
fi
if [ ! -f "${publishdir}/scalus" ]; then 
	echo "publishdir must be full path containing published scalus"
	exit 1
fi


echo "Building from ${infile}"
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
		<key>CFBundleTypeRole</key>
		<string>Viewer</string>
		<key>CFBundleURLName</key>
		<string>scalus telnet URL</string>
		<key>CFBundleURLSchemes</key>
		<array>
			<string>telnet</string>
		</array>
	</dict>
	<dict>
		<key>CFBundleTypeRole</key>
		<string>Viewer</string>
		<key>CFBundleURLName</key>
		<string>scalus ssh URL</string>
		<key>CFBundleURLSchemes</key>
		<array>
			<string>ssh</string>
		</array>
	</dict>
	<dict>
		<key>CFBundleTypeRole</key>
		<string>Viewer</string>
		<key>CFBundleURLName</key>
		<string>scalus rdp URL</string>
		<key>CFBundleURLSchemes</key>
		<array>
			<string>rdp</string>
		</array>
	</dict>
</array> '"


/bin/bash -c "defaults write $filename CFBundleVersion  -string \"${version}\""
/bin/bash -c "defaults write $filename CFBundleShortVersion  -string \"${version}\""
/bin/bash -c "defaults write $filename CFBundleName  -string 'Scalus'"
/bin/bash -c "defaults write $filename CFBundleDisplayName  -string 'Session URL Launcher Utility'"
/bin/bash -c "defaults write $filename CFBundleIdentifier  -string  'com.oneidentity.${appname}.macos'"
/bin/bash -c "defaults write $filename ITSAppUsesNonExemptEncryption -bool false"
/bin/bash -c "defaults write $filename LSApplicationCategoryType -string 'public.app-category.developer-tools'"
/bin/bash -c "defaults write $filename LSMinimumSystemVersion -string '10.6.0'"

    chmod a+r $filename
}

function make_app()
{
	if [ -d ${tmpdir} ]; then 
        	rm -rf ${tmpdir}
	fi
        mkdir -p ${tmpdir}

        cat ${infile} | awk -v appname="com.oneidentity.${appname}.macos" '
{
        str=sprintf("kMDItemCFBundleIdentifier=%s", appname);
        sub(/kMDItemCFBundleIdentifier=\S+/, str);
	print $0
}' > ${infile}.tmp
if [ $? -eq 0 ]; then 
	mv ${infile}.tmp ${infile}
fi

        osacompile -o ${tmpdir}/${appname}.app ${infile}
        resetInfo

	cp $publishdir/scalus  ${tmpdir}/${appname}.app/Contents/MacOS
	chmod u=rwx,go=rx  ${tmpdir}/${appname}.app/Contents/MacOS/scalus

	mkdir -p ${tmpdir}/${appname}.app/Contents/Resources/examples
	chmod a+rx ${tmpdir}/${appname}.app/Contents/Resources/Examples

	cp $publishdir/examples/*  ${tmpdir}/${appname}.app/Contents/Resources/examples
	chmod a+r ${tmpdir}/${appname}.app/Contents/Resources/examples/*
	here=`pwd`
	cd $tmpdir
	tar -cvf - ${appname}.app | gzip -c > ${pkgtarfile}
	cd $here

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
