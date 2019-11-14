#!/bin/bash
#usage: deploy-both.sh <build_path> [<specified_version>]
#end

if [[ "$1" == "-h" ]]
then
	sed -n '2,/#end/ p' "$0"
	exit 0
fi

BUILD=${1:-"../Builds"}
SPECIFIED=*
if [[ ! -z "$2" ]]
then
	SPECIFIED=*"${2}"*
fi

if [[ ! -d "$BUILD" ]]; then
    echo "Path '$BUILD' does not exist!"
    exit 1
fi

declare -A PLATFORMS
PLATFORMS[Linux64]=linux
PLATFORMS[Win64]=win 

unset -v latest
for dir in "$BUILD"/${SPECIFIED}; do
    for platform in "${!PLATFORMS[@]}"; do 
        [[ ${dir} =~ .*\.${platform} && ${dir} -nt ${latest} ]] && latest=${dir}
    done
done

for platform in "${!PLATFORMS[@]}"; do
    echo
    echo "${latest%.*}.${platform}"
    sh ./deploy.sh ${PLATFORMS[$platform]} "${latest%.*}.${platform}" 
done