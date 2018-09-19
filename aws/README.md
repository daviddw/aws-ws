### Creating an EC2 host

```aws cloudformation create-stack --stack-name ec2-test-stack --template-body file://ec2.yaml --parameters ParameterKey=KeyName,ParameterValue=ssh_key```

### Accessing an EC2 host

```ssh -i ssh_key ec2-user@host```

### Creating an ECS stack

```aws cloudformation create-stack --stack-name ecs-test-stack --template-body file://ecs.yaml --capabilities CAPABILITY_IAM --parameters ParameterKey=KeyName,ParameterValue=ssh_key ParameterKey=VpcId,ParameterValue=vpc_id 'ParameterKey=SubnetIds,ParameterValue="subnet_id1, subnet_id2, ..."' ParameterKey=DockerImage,ParameterValue=docker_image```

### Deleting an AWS stack

aws cloudformation delete-stack --stack-name stack_name

