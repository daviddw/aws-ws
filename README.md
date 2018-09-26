# aws-ws

[![Build status](https://ci.appveyor.com/api/projects/status/fg8b6w5ht84iyemx/branch/master?svg=true)](https://ci.appveyor.com/project/DavidDrysdaleWilson/aws-ws/branch/master)

WebSocket Server backed by AWS

## Environment variables

The following environment variables are required for the service to run,

* $CONFIGURATION
* $DOCKERUSER
* $DOCKERTAG
* $AWSKEYNAME
* $AWSVPCID
* $AWSSUBNETS

## Build targets

`./build.sh -t Build`

Builds the WebSocket service

`./build.sh -t Publish'

Builds the service's Docker image and pushes to DockerHub

`./build.sh -t Deploy`

Deploys the SNS/SQS and the ECS stacks to AWS

`./build.sh -t DeleteStack`

Deletes the SNS/SQS and the ECS stacks from AWS
