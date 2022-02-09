FROM microsoft/dotnet:2.1-sdk AS build

COPY *.csproj /build/
RUN dotnet restore ./build/Authorization.Service.csproj
COPY . ./build/
WORKDIR /build/
RUN dotnet build ./Authorization.Service.csproj -c Release -o out
RUN dotnet publish ./Authorization.Service.csproj -c Release -o out

FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app

COPY --from=build /build/out .
ENTRYPOINT ["dotnet", "Authorization.Services.dll"]

# install System.Drawing native dependencies
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
     && rm -rf /var/lib/apt/lists/*