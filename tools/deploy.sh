#!/bin/bash
BUILD="../Builds"

if [[ ! -d "${BUILD}" ]]; then
  echo "Path '${BUILD}' does not exist!"
  exit 1
fi

eval cd "${BUILD}"
VERSION=$(<version)
echo ${VERSION}

ARCHIVE_NAME=sandbox-linux-${VERSION}.zip
rm ${ARCHIVE_NAME}
zip -r ${ARCHIVE_NAME} * --exclude *.zip

scp ${ARCHIVE_NAME} konstantin@office.brandymint.ru:/home/konstantin/SandyAppBuilds

git tag v${VERSION}