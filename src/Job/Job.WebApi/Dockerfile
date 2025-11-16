# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS src

WORKDIR /src
COPY src .

WORKDIR /src/Job/Job.WebApi
RUN dotnet publish Job.WebApi.csproj -c Release -r linux-x64 -o /out


# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

WORKDIR /app
COPY --from=src /out .

EXPOSE 80/tcp 443/tcp

ENTRYPOINT ["dotnet"]
CMD ["Job.WebApi.dll"]
