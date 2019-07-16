#addin Cake.AWS.S3&version=0.6.8
#addin Cake.Docker&version=0.10.0

var date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");

var stackName = "aws-service-test";

var vcsRef = EnvironmentVariable("VCSREF") ?? "";
var vcsBranch = EnvironmentVariable("VCSBRANCH") ?? "";

var tag = $"{vcsRef}-{vcsBranch}".Replace('/', '-');

var isMasterBranch = vcsBranch == "master";

var deploymentState = isMasterBranch ? "prod" : "dev";

var bucketName = EnvironmentVariable("S3BUCKET") ?? "";

var defaultRegion = EnvironmentVariable("AWS_DEFAULT_REGION") ?? "";
var secretKey = EnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "";
var accessKey = EnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "";

var sshKey = EnvironmentVariable("AWSKEYNAME") ?? "";
var vpcId = EnvironmentVariable("AWSVPCID") ?? "";
var subnets = EnvironmentVariable("AWSSUBNETS") ?? "";

var dockerUser = EnvironmentVariable("DOCKERUSER") ?? "";

var dockerImageBranchTag = $"{dockerUser}/{stackName}:{tag}";
var dockerImageLatestTag = $"{dockerUser}/{stackName}:latest";
var dockerImageTags = isMasterBranch ? new [] { dockerImageBranchTag, dockerImageLatestTag } : new [] { dockerImageBranchTag };

var lambdaFilename = $"aws-lambda-test-{tag}.zip";

var target = Argument("target", "Default");

Task("Default")
  .IsDependentOn("Clean")
  .IsDependentOn("Deploy");

Task("Clean")
  .Does(() => {
    if (DirectoryExists("./src/aws-lambda-test/bin"))
    {
      DeleteDirectory("./src/aws-lambda-test/bin", new DeleteDirectorySettings {
        Recursive = true
      });
    }
    
    if (DirectoryExists("./src/aws-lambda-test/obj"))
    {
      DeleteDirectory("./src/aws-lambda-test/obj", new DeleteDirectorySettings {
        Recursive = true
      });
    }

    if (DirectoryExists("./deploy"))
    {
      DeleteDirectory("./deploy", new DeleteDirectorySettings {
        Recursive = true
      });
    }
  });

Task("Build")
  .Does(() => {
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug"
    };

    DotNetCoreBuild("./src/", settings);
  });

Task("Publish-Service")
  .Does(() => {
    var settings = new DotNetCorePublishSettings
    {
        Configuration = "Release",
    };

    DotNetCorePublish("./src/", settings);
  });

Task("Publish-Lambda")
  .Does(() => {
    CreateDirectory("deploy");

    Zip("./src/aws-lambda-test/bin/Release/netcoreapp2.1/publish", $"./deploy/{lambdaFilename}");
  });

Task("Publish-Docker")
  .IsDependentOn("Publish-Service")
  .Does(() => {
    var settings = new DockerImageBuildSettings {
        NoCache = true,
        BuildArg = new [] { $"VCSREF={vcsRef}", $"VERSION={tag}", $"BUILDDATE={date}" },
        Tag = dockerImageTags,
        File = "./src/Dockerfile"
    };

    DockerBuild(settings, ".");
  });

Task("Deploy-Docker")
  .IsDependentOn("Publish-Docker")
  .Does(() => {
      if (isMasterBranch) {
        DockerPush(dockerImageBranchTag);
        Console.WriteLine($"Published {dockerImageBranchTag}");
      }
      else {
        Console.WriteLine($"Only push to Docker from CI");
      }
  });

Task("Deploy-Lambda")
  .IsDependentOn("Publish-Lambda")
  .Does(async () => {
    await S3Upload($"./deploy/{lambdaFilename}", $"{lambdaFilename}",
      new UploadSettings()
        .SetAccessKey(accessKey)
        .SetSecretKey(secretKey)
        .SetRegion(defaultRegion)
        .SetBucketName(bucketName));
    
    Console.WriteLine($"Published {bucketName}/{lambdaFilename}");
  });

Task("Deploy-Stack")
  .Does(() => {
    if (string.IsNullOrWhiteSpace(vcsBranch) || isMasterBranch)
    {
      var result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"cloudformation deploy --stack-name {stackName}-queue --template-file sqs.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
        throw new Exception("aws cloudformation deploy failed.");
      }

      result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"cloudformation deploy --stack-name {stackName}-lambda --template-file lambda.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
        throw new Exception("aws cloudformation deploy failed.");
      }

      result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"cloudformation deploy --stack-name {stackName}-ecs --template-file ecs.yaml --capabilities CAPABILITY_IAM --parameter-overrides KeyName={sshKey} VpcId={vpcId} SubnetIds={subnets} DockerImage={dockerImageTags} DockerTag={tag}",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
          throw new Exception("aws cloudformation deploy failed.");
      }

      result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"cloudformation describe-stacks --stack-name aws-service-test-ecs --query 'Stacks[0].Outputs[0].OutputValue'",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
          throw new Exception("aws cloudformation describe-stacks failed.");
      }
    }
    else
    {
        Console.WriteLine($"aws cloudformation deploy --stack-name {stackName}-ecs --template-file ecs.yaml --capabilities CAPABILITY_IAM --parameter-overrides KeyName={sshKey} VpcId={vpcId} SubnetIds={subnets} DockerImage={dockerImageTags} DockerTag={tag}");
    }
  });

Task("Deploy")
  .IsDependentOn("Deploy-Docker")
  .IsDependentOn("Deploy-Lambda")
  .IsDependentOn("Deploy-Stack")
  .Does(() => {
  });

Task("Recall")
  .Does(() => {
      WaitStackDelete(Context, $"{stackName}-ecs");
      Console.WriteLine($"Deleted stack {stackName}-ecs");
      WaitStackDelete(Context, $"{stackName}-lambda");
      Console.WriteLine($"Deleted stack {stackName}-lambda");
      WaitStackDelete(Context, $"{stackName}-queue");
      Console.WriteLine($"Deleted stack {stackName}-queue");
  });

public static int RunCommand(ICakeContext context, string command, ProcessSettings settings = null) {
  if (settings == null) {
    settings = new ProcessSettings();
  }

  if (context.IsRunningOnUnix()) {
    return context.StartProcess(command, settings);
  } else {
    settings.Arguments.Prepend($"/c \"{command}\"");

    return context.StartProcess("cmd", settings);
  }
}

public static void WaitStackDelete(ICakeContext context, string stack) {
  var result = RunCommand(context, "aws", new ProcessSettings {
    Arguments = $"cloudformation delete-stack --stack-name {stack}",
    WorkingDirectory = new DirectoryPath("./aws/")
  });

  if (result != 0) {
    throw new Exception("aws cloudformation delete-stack failed.");
  }

  result = RunCommand(context, "aws", new ProcessSettings {
    Arguments = $"cloudformation wait stack-delete-complete --stack-name {stack}",
    WorkingDirectory = new DirectoryPath("./aws/")
  });

  if (result != 0) {
    throw new Exception("aws wait stack-delete-complete failed.");
  };
}

RunTarget(target);
