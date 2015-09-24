FROM microsoft/aspnet

COPY project.json /app/
WORKDIR /app
ENV MONO_THREADS_PER_CPU=2000
RUN ["dnu", "restore"]
ENV MONO_THREADS_PER_CPU=100
COPY . /app

EXPOSE 5004
ENTRYPOINT ["dnx", "project.json", "kestrel"]
