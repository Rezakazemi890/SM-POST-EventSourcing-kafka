#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5006
#EXPOSE 5010

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Post.Authorization/Post.Authorizaton.Api/Post.Authorization.Api.csproj", "Post.Authorization/Post.Authorizaton.Api/"]
COPY ["Post.Authorization/Post.Authorization.Infrastructure/Post.Authorization.Infrastructure.csproj", "Post.Authorization/Post.Authorization.Infrastructure/"]
COPY ["Post.Authorization/Post.Authorization.Domain/Post.Authorization.Domain.csproj", "Post.Authorization/Post.Authorization.Domain/"]
COPY ["Post.Common/Post.Common.csproj", "Post.Common/"]
COPY ["CQRS-ES/CQRS.Core/CQRS.Core.csproj", "CQRS-ES/CQRS.Core/"]
RUN dotnet restore "Post.Authorization/Post.Authorizaton.Api/Post.Authorization.Api.csproj"
COPY . .
WORKDIR "/src/Post.Authorization/Post.Authorizaton.Api"
RUN dotnet build "Post.Authorization.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Post.Authorization.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Post.Authorization.Api.dll"]
