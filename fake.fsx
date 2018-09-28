#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.Core.Process
nuget Fake.Core.Target //"
#load "./.fake/fake.fsx/intellisense.fsx"

open System
open System.IO
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators

// Properties
let projectName = "aws-service-test"

let vcsRef = Environment.environVarOrDefault "VCSREF" ""
let buildDate = Environment.environVarOrDefault "BUILDDATE" ""
let configuration = Environment.environVarOrDefault "CONFIGURATION" ""

let baseDir = __SOURCE_DIRECTORY__
let sourceDir = Path.combine baseDir "src"

let serviceDir = Path.combine sourceDir "aws-service-test"
let serviceBuildDir = Path.combine serviceDir (Path.combine "bin" configuration) |> Path.GetFullPath
let serviceDeployDir = Path.combine serviceDir (Path.combine "deploy" configuration) |> Path.GetFullPath

let lambdaDir = Path.combine sourceDir "aws-lambda-test"
let lambdaBuildDir = Path.combine lambdaDir (Path.combine "bin" configuration) |> Path.GetFullPath
let lambdaDeployDir = Path.combine lambdaDir (Path.combine "deploy" configuration) |> Path.GetFullPath

let dockerUser = Environment.environVarOrDefault "DOCKERUSER" ""
let tag = Environment.environVarOrDefault "DOCKERTAG" "" |> fun s -> s.Replace('/', '-')

let dockerImage = dockerUser + "/" + projectName + ":" + tag

let stackName = "aws-service-test"

let awsDir = Path.combine baseDir "aws"
let sshKey = Environment.environVarOrDefault "AWSKEYNAME" ""
let vpcId = Environment.environVarOrDefault "AWSVPCID" ""
let subnets = Environment.environVarOrDefault "AWSSUBNETS" ""


// Targets
Target.create "Clean" (fun _ ->
    Shell.cleanDirs
        [ serviceBuildDir
          serviceDeployDir
          lambdaBuildDir
          lambdaDeployDir ]
)

Target.create "Build" (fun _ ->
    DotNet.build (fun c ->
        { c with
            Configuration = DotNet.BuildConfiguration.Custom(configuration)
            OutputPath = Some(serviceBuildDir)
        }) serviceDir

    DotNet.build (fun c ->
        { c with
            Configuration = DotNet.BuildConfiguration.Custom(configuration)
            OutputPath = Some(lambdaBuildDir)
        }) lambdaDir
)

Target.create "Publish" (fun _ ->
    DotNet.publish (fun c ->
        { c with
            Common = DotNet.Options.Create() |> (fun c ->
                { c with
                    CustomParams = Some("/property:PublishWithAspNetCoreTargetManifest=false")
                })
            Configuration = DotNet.BuildConfiguration.Release;
            OutputPath = Some(serviceDeployDir)
        }) serviceDir

    DotNet.publish (fun c ->
        { c with
            Common = DotNet.Options.Create() |> (fun c ->
                { c with
                    CustomParams = Some("/property:PublishWithAspNetCoreTargetManifest=false")
                })
            Configuration = DotNet.BuildConfiguration.Release;
            OutputPath = Some(lambdaDeployDir)
        }) lambdaDir
)

Target.create "PublishDocker" (fun _ ->
    ignore(Shell.Exec("docker", "build -f Dockerfile -t " + dockerImage + " --build-arg VCSREF=" + vcsRef + " --build-arg VERSION=" + tag + " --build-arg BUILDDATE=" + buildDate + " .", sourceDir))
)

Target.create "PushDocker" (fun _ ->
    ignore(Shell.Exec("docker", "push " + dockerUser + "/" + projectName, sourceDir))
)

Target.create "Deploy" (fun _ ->
    ignore(Shell.Exec("aws", "cloudformation deploy --stack-name " + stackName + "-lambda --template-file lambda.yaml", awsDir))

    ignore(Shell.Exec("aws", "cloudformation deploy --stack-name " + stackName + "-queue --template-file sqs.yaml", awsDir))

    ignore(Shell.Exec("aws", "cloudformation deploy --stack-name " + stackName + "-ecs --template-file ecs.yaml --capabilities CAPABILITY_IAM --parameter-overrides KeyName=" + sshKey + " VpcId=" + vpcId + " SubnetIds=" + subnets + " DockerImage=" + dockerImage + " DockerTag=" + tag, awsDir))
)

Target.create "DeleteStack" (fun _ ->
    ignore(Shell.Exec("aws", "cloudformation delete-stack --stack-name " + stackName + "-ecs", awsDir))

    ignore(Shell.Exec("aws", "cloudformation wait stack-delete-complete --stack-name "+ stackName + "-ecs", awsDir))

    ignore(Shell.Exec("aws", "cloudformation delete-stack --stack-name " + stackName + "-queue", awsDir))

    ignore(Shell.Exec("aws", "cloudformation wait stack-delete-complete --stack-name "+ stackName + "-queue", awsDir))

    ignore(Shell.Exec("aws", "cloudformation delete-stack --stack-name " + stackName + "-lambda", awsDir))
)

open Fake.Core.TargetOperators

// Dependencies
"Clean"
    ==> "Build"
    ==> "Publish"

"Publish"
    ==> "PublishDocker"

"PublishDocker"
    ==> "PushDocker"

// start build
Target.runOrDefaultWithArguments "PublishDocker"
