FROM microsoft/dotnet:2.0-runtime

EXPOSE 3000

WORKDIR /app
COPY publish/ .
COPY launch.sh .

ENTRYPOINT ["/bin/bash", "launch.sh"] 
