#!/bin/sh

olddir=`pwd`

cd "${0%/*}" # ensure cwd is script dir

buildpackages=$(pwd)/buildpackages
fake=$buildpackages/fake

if [ ! -f $fake ]; then
  echo Installing FAKE
  dotnet tool install fake-cli --tool-path $buildpackages --version 5.*
fi

#if [ -f .fake ]; then
#  rm -rf .fake
#fi

#if [ -f fake.fsx.lock ]; then
#  rm fake.fsx.lock
#fi

$fake run fake.fsx "$@"

cd $olddir
