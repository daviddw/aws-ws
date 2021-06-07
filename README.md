# aws-ws

![Deploy status](https://github.com/daviddw/aws-ws/actions/workflows/deploy.yml/badge.svg) [![](https://images.microbadger.com/badges/version/dgdw/aws-service-test.svg)](https://microbadger.com/images/dgdw/aws-service-test "Get your own version badge on microbadger.com")

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

Builds the WebSocket service and Lambda function

`./build.sh -t Deploy`

Builds and deploys the service's Docker image, Lambda function and stacks

`./build.sh -t Recall`

Deletes the stacks
