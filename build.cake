#addin Cake.AWS.S3&version=1.0.0&loaddependencies=true
#addin Cake.Docker&version=1.0.0

var date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");

var stackName = "aws-service-test";

var vcsRef = EnvironmentVariable("VCSREF") ?? "";
var vcsBranch = EnvironmentVariable("VCSBRANCH")?.Split('/').Last() ?? "";

var tag = $"{vcsRef}-{vcsBranch}".Replace('/', '-');

var isMasterBranch = string.IsNullOrWhiteSpace(vcsBranch) || (vcsBranch == "master");

var deploymentState = isMasterBranch ? "prod" : "dev";

var bucketName = EnvironmentVariable("S3BUCKET") ?? "";

var defaultRegion = EnvironmentVariable("AWS_DEFAULT_REGION") ?? "";
var secretKey = EnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "";
var accessKey = EnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "";

var sshKey = EnvironmentVariable("AWSKEYNAME") ?? "";
var vpcId = EnvironmentVariable("AWSVPCID") ?? "";
var subnets = EnvironmentVariable("AWSSUBNETS") ?? "";

var dockerUser = EnvironmentVariable("DOCKERUSER") ?? "";

var dockerImage = $"{dockerUser}/{stackName}";
var dockerImageBranchTag = $"{dockerUser}/{stackName}:{tag}";
var dockerImageLatestTag = $"{dockerUser}/{stackName}:latest";
var dockerImageTags = isMasterBranch ? new [] { dockerImageBranchTag, dockerImageLatestTag } : new [] { dockerImageBranchTag };

var lambdaFilename = $"aws-lambda-test-{tag}.zip";

var target = Argument("target", "Default");

var profile = Argument("Profile", "");

if(!string.IsNullOrEmpty(profile))
{
    profile = $"--profile {profile} ";
}

Task("Default")
  .IsDependentOn("Clean")
  .IsDependentOn("Deploy");

Task("Clean")
  .Does(() => {
    var dirs = new string[] { "aws-service-test", "aws-lambda-test" };

    foreach(var dir in dirs)
    {
        if (DirectoryExists($"./src/{dir}/bin"))
        {
            DeleteDirectory($"./src/{dir}/bin", new DeleteDirectorySettings { Recursive = true});
        }
        
        if (DirectoryExists($"./src/{dir}/obj"))
        {
            DeleteDirectory($"./src/{dir}/obj", new DeleteDirectorySettings { Recursive = true});
        }

        if (DirectoryExists($"./src/{dir}/deploy"))
        {
            DeleteDirectory($"./src/{dir}/deploy", new DeleteDirectorySettings { Recursive = true});
        }
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
  .IsDependentOn("Publish-Service")
  .Does(() => {
    CreateDirectory("./src/aws-lambda-test/deploy");

    Zip("./src/aws-lambda-test/bin/Release/netcoreapp2.1/publish", $"./src/aws-lambda-test/deploy/{lambdaFilename}");
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
    DockerPush(dockerImageBranchTag);
    if (isMasterBranch) {
      DockerPush(dockerImageLatestTag);
    }

    Console.WriteLine($"Published {dockerImage}");
  });

Task("Deploy-Lambda")
  .IsDependentOn("Publish-Lambda")
  .Does(async () => {
    await S3Upload($"./src/aws-lambda-test/deploy/{lambdaFilename}", $"{lambdaFilename}",
      new UploadSettings()
        .SetAccessKey(accessKey)
        .SetSecretKey(secretKey)
        .SetRegion(defaultRegion)
        .SetBucketName(bucketName));
    
    Console.WriteLine($"Published {bucketName}/{lambdaFilename}");
  });

Task("Deploy-Stack")
  .Does(() => {
    if (isMasterBranch)
    {
      var result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"{profile}cloudformation deploy --stack-name {stackName}-queue --template-file sqs.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
        throw new Exception($"aws {profile}cloudformation deploy failed.");
      }

      result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"{profile}cloudformation deploy --stack-name {stackName}-lambda --template-file lambda.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
        throw new Exception($"aws {profile}cloudformation deploy failed.");
      }

      result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"{profile}cloudformation deploy --stack-name {stackName}-ecs --template-file ecs.yaml --capabilities CAPABILITY_IAM --parameter-overrides KeyName={sshKey} VpcId={vpcId} SubnetIds={subnets} DockerImage={dockerImageBranchTag} DockerTag={tag}",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
          throw new Exception($"aws {profile}cloudformation deploy failed.");
      }

      result = RunCommand(Context, "aws", new ProcessSettings {
          Arguments = $"{profile}cloudformation describe-stacks --stack-name {stackName}-ecs --query 'Stacks[0].Outputs[0].OutputValue'",
          WorkingDirectory = new DirectoryPath("./aws/")
      });

      if (result != 0) {
          throw new Exception($"aws {profile}cloudformation describe-stacks failed.");
      }
    }
    else
    {
        Console.WriteLine($"aws {profile}cloudformation deploy --stack-name {stackName}-queue --template-file sqs.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}");
        Console.WriteLine($"aws {profile}cloudformation deploy --stack-name {stackName}-lambda --template-file lambda.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}");
        Console.WriteLine($"aws {profile}cloudformation deploy --stack-name {stackName}-ecs --template-file ecs.yaml --capabilities CAPABILITY_IAM --parameter-overrides KeyName={sshKey} VpcId={vpcId} SubnetIds={subnets} DockerImage={dockerImageBranchTag} DockerTag={tag}");
        Console.WriteLine($"aws {profile}cloudformation describe-stacks --stack-name {stackName}-ecs --query 'Stacks[0].Outputs[0].OutputValue'");
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
      WaitStackDelete(Context, $"{stackName}-ecs", profile);
      Console.WriteLine($"Deleted stack {stackName}-ecs");
      WaitStackDelete(Context, $"{stackName}-lambda", profile);
      Console.WriteLine($"Deleted stack {stackName}-lambda");
      WaitStackDelete(Context, $"{stackName}-queue", profile);
      Console.WriteLine($"Deleted stack {stackName}-queue");
  });

Task("Setup")
 .Does(() => {
    var settings = new DockerComposeUpSettings {
      Files = new [] { "test/infrastructure/docker-compose.yaml" },
      DetachedMode = true,
      Build = true
    };

    DockerComposeUp(settings);

    Console.WriteLine($"{profile}cloudformation create-stack --stack-name {stackName}-queue --endpoint-url http://localhost:4581 --template-file sqs.yaml --capabilities CAPABILITY_IAM");
    
    RunCommand(Context, "aws",  new ProcessSettings {
      Arguments = $"{profile}cloudformation create-stack --stack-name {stackName}-queue --endpoint-url http://localhost:4581 --template-file=sqs.yaml",
      WorkingDirectory = new DirectoryPath("./aws/")
    });

    Console.WriteLine($"{profile}cloudformation deploy --stack-name {stackName}-lambda --endpoint-url http://localhost:4581 --region us-east-1 --template-file lambda.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}");
    
    RunCommand(Context, "aws",  new ProcessSettings {
      Arguments = $"{profile}cloudformation deploy --stack-name {stackName}-lambda --endpoint-url http://localhost:4581 --region us-east-1 --template-file lambda.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}",
      WorkingDirectory = new DirectoryPath("./aws/")
    });

    //Arguments = $"{profile}sqs create-queue --queue-name {stackName}-queue --endpoint-url http://localhost:4576 --region us-east-1",
      //Arguments = $"{profile}cloudformation deploy --stack-name {stackName}-lambda --template-file lambda.yaml --capabilities CAPABILITY_IAM --parameter-overrides BucketName={bucketName} LambdaPackage={lambdaFilename}",
  });

Task("Teardown")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Files = new [] { "test/infrastructure/docker-compose.yaml" }
    };

    DockerComposeDown(settings);
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

public static void WaitStackDelete(ICakeContext context, string stack, string profile) {
  var result = RunCommand(context, "aws", new ProcessSettings {
    Arguments = $"{profile}cloudformation delete-stack --stack-name {stack}",
    WorkingDirectory = new DirectoryPath("./aws/")
  });

  if (result != 0) {
    throw new Exception("aws cloudformation delete-stack failed.");
  }

  result = RunCommand(context, "aws", new ProcessSettings {
    Arguments = $"{profile}cloudformation wait stack-delete-complete --stack-name {stack}",
    WorkingDirectory = new DirectoryPath("./aws/")
  });

  if (result != 0) {
    throw new Exception("aws wait stack-delete-complete failed.");
  };
}

RunTarget(target);
