aws cloudformation create-stack --stack-name ec2-test-stack --template-body file://aws/ec2.yaml --parameters ParameterKey=KeyName,ParameterValue=<ssh key>

aws cloudformation delete-stack --stack-name ec2-test-stack

aws cloudformation create-stack --stack-name ecs-test-stack --template-body file://ecs.yaml --capabilities CAPABILITY_IAM --parameters ParameterKey=KeyName,ParameterValue=<ssh key> ParameterKey=VpcId,ParameterValue=<vpc id> 'ParameterKey=SubnetIds,ParameterValue="<subnet id1>, <subnet id2>, ..."' ParameterKey=DockerImage,ParameterValue=d<docker image>

aws cloudformation delete-stack --stack-name ecs-test-stack

ssh -i <key> ec2-user@<host>

