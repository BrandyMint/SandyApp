#!/bin/bash
#usage: deploy.sh <platform> <build_path>
#end

if [[ "$1" == "-h" ]]
then
	sed -n '2,/#end/ p' "$0"
	exit 0
fi

PLATFORM=${1:-linux}
BUILD=${2:-"../Builds"}

if [[ ! -d "$BUILD" ]]; then
  echo "Path '$BUILD' does not exist!"
  exit 1
fi

cd "$BUILD"
VERSION=$(<version)
echo ${VERSION}

ARCHIVE_NAME=sandbox-${PLATFORM}-${VERSION}.zip
rm ${ARCHIVE_NAME}
7z a ${ARCHIVE_NAME} * -x!*.zip -x!*.Linux64\* -x!*.Win64\*

scp ${ARCHIVE_NAME} konstantin@office.brandymint.ru:/home/konstantin/SandyAppBuilds

git tag v${VERSION}