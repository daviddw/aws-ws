FROM microsoft/dotnet:2.0-runtime

EXPOSE 3000

WORKDIR /app

COPY aws-service-test/deploy/Release .
COPY launch.sh .

ARG VCSREF
ARG VERSION
ARG BUILDDATE

LABEL org.label-schema.vendor="David Drysdale-Wilson" \
      org.label-schema.build-date=$BUILDDATE \
	  org.label-schema.docker.dockerFile="Dockerfile" \
	  org.label-schema.version=$VERSION \
	  org.label-schema.vcs-ref=$VCSREF \
	  org.label-schema.vcs=type="Git" \
	  org.label-schema.vcs-url="https://github.com/daviddw/aws-ws" \
	  org.label-schema.name="WebSocket Server backed by AWS"

ENTRYPOINT ["/bin/bash", "launch.sh"] 
