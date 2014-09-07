#!/bin/bash

if [ "x${1}x" == "xx" ]; then
	echo "Version number required."
	exit 1
fi

mkdir Release

rm -Rf Release/OrbitalMaterialScienceLab_${1}.zip
zip -r Release/OrbitalMaterialScienceLab_${1}.zip GameData
zip Release/OrbitalMaterialScienceLab_${1}.zip README.md
zip Release/OrbitalMaterialScienceLab_${1}.zip LICENSE
