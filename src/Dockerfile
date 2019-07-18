FROM microsoft/dotnet:2.1-runtime

EXPOSE 3000

WORKDIR /app

COPY src/aws-service-test/bin/Release/netcoreapp2.1/publish/ .
COPY src/launch.sh .

ARG VCSREF
ARG VERSION
ARG BUILDDATE

LABEL org.label-schema.vendor="David Drysdale-Wilson" \
      org.label-schema.build-date=$BUILDDATE \
      org.label-schema.docker.dockerFile="Dockerfile" \
      org.label-schema.version=$VERSION \
      org.label-schema.vcs-ref=$VCSREF \
      org.label-schema.vcs-type="git" \
      org.label-schema.vcs-url="https://github.com/daviddw/aws-ws" \
      org.label-schema.name="aws-service-test" \
      org.label-schema.description="WebSocket Server backed by AWS"

ENTRYPOINT ["/bin/bash", "launch.sh"] 
