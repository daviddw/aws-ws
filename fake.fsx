#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.IO.Zip
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

let date = DateTime.Now.ToString "yyyy-MM-ddTHH:mm:ssZ"

// Properties
let projectName = "aws-service-test"

let vcsRef = Environment.environVarOrDefault "VCSREF" ""
let vcsBranch = Environment.environVarOrDefault "VCSBRANCH" ""
let buildDate = Environment.environVarOrDefault "BUILDDATE" date
let configuration = Environment.environVarOrDefault "CONFIGURATION" ""

let baseDir = __SOURCE_DIRECTORY__
let sourceDir = Path.combine baseDir "src"

let serviceDir = Path.combine sourceDir "aws-service-test"
let serviceBuildDir = Path.combine serviceDir (Path.combine "bin" configuration) |> Path.GetFullPath
let serviceDeployDir = Path.combine serviceDir (Path.combine "deploy" configuration) |> Path.GetFullPath

let lambdaDir = Path.combine sourceDir "aws-lambda-test"
let lambdaBuildDir = Path.combine lambdaDir (Path.combine "bin" configuration) |> Path.GetFullPath
let lambdaDeployDir = Path.combine lambdaDir (Path.combine "deploy" configuration) |> Path.GetFullPath
let lambdaPackageDir = Path.combine lambdaDir "deploy" |> Path.GetFullPath
let lambdaPackageFilename = "aws-lambda-test.zip"

let dockerUser = Environment.environVarOrDefault "DOCKERUSER" ""
let tag = vcsRef + "-" + vcsBranch |> fun s -> s.Replace('/', '-')

let dockerImage = dockerUser + "/" + projectName + ":" + tag

let bucketName = Environment.environVarOrDefault "S3BUCKET" ""

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

    let files = Directory.EnumerateFiles(lambdaDeployDir)
    Fake.IO.Zip.zip lambdaDeployDir (lambdaPackageDir + "/" + lambdaPackageFilename) files

    ignore(Shell.Exec("docker", "build -f Dockerfile -t " + dockerImage + " --build-arg VCSREF=" + vcsRef + " --build-arg VERSION=" + tag + " --build-arg BUILDDATE=" + buildDate + " .", sourceDir))
)

Target.create "PushLambda" (fun _ ->
    ignore(Shell.Exec("aws", "s3 cp " + lambdaPackageFilename + " s3://" + bucketName, lambdaPackageDir))
)

Target.create "PushDocker" (fun _ ->
    ignore(Shell.Exec("docker", "push " + dockerUser + "/" + projectName, sourceDir))
)

Target.create "StartStacks" (fun _ ->
    ignore(Shell.Exec("aws", "cloudformation deploy --stack-name " + stackName + "-queue --template-file sqs.yaml", awsDir))

    ignore(Shell.Exec("aws", "cloudformation deploy --stack-name " + stackName + "-lambda --template-file lambda.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName=" + bucketName + " LambdaPackage=" + lambdaPackageFilename, awsDir))

    ignore(Shell.Exec("aws", "cloudformation deploy --stack-name " + stackName + "-ecs --template-file ecs.yaml --capabilities CAPABILITY_IAM --parameter-overrides KeyName=" + sshKey + " VpcId=" + vpcId + " SubnetIds=" + subnets + " DockerImage=" + dockerImage + " DockerTag=" + tag, awsDir))
)

Target.create "StopStacks" (fun _ ->
    ignore(Shell.Exec("aws", "cloudformation delete-stack --stack-name " + stackName + "-ecs", awsDir))

    ignore(Shell.Exec("aws", "cloudformation wait stack-delete-complete --stack-name "+ stackName + "-ecs", awsDir))

    ignore(Shell.Exec("aws", "cloudformation delete-stack --stack-name " + stackName + "-lambda", awsDir))

    ignore(Shell.Exec("aws", "cloudformation wait stack-delete-complete --stack-name "+ stackName + "-lambda", awsDir))

    ignore(Shell.Exec("aws", "cloudformation delete-stack --stack-name " + stackName + "-queue", awsDir))

    ignore(Shell.Exec("aws", "cloudformation wait stack-delete-complete --stack-name "+ stackName + "-queue", awsDir))
)

open Fake.Core.TargetOperators

// Dependencies
"Clean"
    ==> "Build"
    ==> "Publish"

"Publish"
    ==> "PushLambda"
    ==> "PushDocker"

// start build
Target.runOrDefaultWithArguments "PushDocker"
