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

let configuration = Environment.getBuildParamOrDefault "CONFIGURATION" ""

let baseDir = __SOURCE_DIRECTORY__
let sourceDir = Path.combine baseDir "src"
let solutionDir = sourceDir
let projectDir = Path.combine sourceDir projectName |> Path.GetFullPath
let buildDir = Path.combine projectDir (Path.combine "bin" configuration) |> Path.GetFullPath
let deployDir = Path.combine projectDir (Path.combine "deploy" configuration) |> Path.GetFullPath

let dockerUser = Environment.environVarOrDefault "DOCKERUSER" ""
let tag = Environment.environVarOrDefault "DOCKERTAG" "" |> fun s -> s.Replace('/', '-')

let dockerImage = dockerUser + "/" + projectName + ":" + tag

let awsDir = Path.combine baseDir "aws"
let sshKey = Environment.environVarOrDefault "AWSKEYNAMW" ""
let vpcId = Environment.environVarOrDefault "AWSVPCID" ""
let subnets = Environment.environVarOrDefault "AWSSUBNETS" ""


// Targets
Target.create "Clean" (fun _ ->
    Shell.cleanDirs
        [ deployDir
          buildDir
          deployDir ]
)

Target.create "Build" (fun _ ->
    DotNet.build (fun c ->
        { c with
            Configuration = DotNet.BuildConfiguration.Custom(configuration)
            OutputPath = Some(buildDir)
        }) solutionDir
)

Target.create "Publish" (fun _ ->
    DotNet.publish (fun c ->
        { c with
            Common = DotNet.Options.Create() |> (fun c ->
                { c with
                    CustomParams = Some("/property:PublishWithAspNetCoreTargetManifest=false")
                })
            Configuration = DotNet.BuildConfiguration.Release;
            OutputPath = Some(deployDir)
        }) solutionDir

    ignore(Shell.Exec("docker", "build -f Dockerfile -t " + dockerImage + " .", sourceDir))

    ignore(Shell.Exec("docker", "push " + dockerUser + "/" + projectName, sourceDir))
)

Target.create "Deploy" (fun _ ->
    ignore(Shell.Exec("aws", "cloudformation deploy --stack-name " + projectName + " --template-file ecs.yaml --capabilities CAPABILITY_IAM --parameter-overrides KeyName=" + sshKey + " VpcId=" + vpcId + " SubnetIds=" + subnets + " DockerImage=" + dockerImage, awsDir))
)

Target.create "DeleteStack" (fun _ ->
    ignore(Shell.Exec("aws", "cloudformation delete-stack --stack-name " + projectName, awsDir))
)

open Fake.Core.TargetOperators

// Dependencies
"Clean"
    ==> "Build"
    ==> "Publish"

// start build
Target.runOrDefaultWithArguments "Build"
