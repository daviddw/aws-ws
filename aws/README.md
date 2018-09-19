aws cloudformation create-stack --stack-name ec2-test-stack --template-body file://aws/ec2.yaml --parameters ParameterKey=KeyName,ParameterValue=ssh_key

aws cloudformation delete-stack --stack-name ec2-test-stack

aws cloudformation create-stack --stack-name ecs-test-stack --template-body file://ecs.yaml --capabilities CAPABILITY_IAM --parameters ParameterKey=KeyName,ParameterValue=ssh_key ParameterKey=VpcId,ParameterValue=vpc_id 'ParameterKey=SubnetIds,ParameterValue="subnet_id1, subnet_id2, ..."' ParameterKey=DockerImage,ParameterValue=docker_image

aws cloudformation delete-stack --stack-name ecs-test-stack

ssh -i ssh_key ec2-user@host

