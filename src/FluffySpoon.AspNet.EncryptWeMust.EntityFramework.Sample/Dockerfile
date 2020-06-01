#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample/FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample.csproj", "FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample/"]
COPY ["FluffySpoon.AspNet.EncryptWeMust.EntityFramework/FluffySpoon.AspNet.EncryptWeMust.EntityFramework.csproj", "FluffySpoon.AspNet.EncryptWeMust.EntityFramework/"]
COPY ["FluffySpoon.AspNet.EncryptWeMust/FluffySpoon.AspNet.EncryptWeMust.csproj", "FluffySpoon.AspNet.EncryptWeMust/"]
RUN dotnet restore "FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample/FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample.csproj"
COPY . .
WORKDIR "/src/FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample"
RUN dotnet build "FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample.dll"]
