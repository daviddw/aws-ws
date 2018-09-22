FROM microsoft/dotnet:2.0-runtime

EXPOSE 3000

WORKDIR /app
COPY aws-service-test/deploy/Release .
COPY launch.sh .

ENTRYPOINT ["/bin/bash", "launch.sh"] 
