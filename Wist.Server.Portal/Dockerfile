#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM microsoft/dotnet:2.2.1-aspnetcore-runtime-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2.103-sdk-nanoserver-1809 AS build
WORKDIR /src
COPY ["../Wist.Server.Portal/Wist.Server.Portal.csproj", "../Wist.Server.Portal/"]
RUN dotnet restore "../Wist.Server.Portal/Wist.Server.Portal.csproj"
COPY . .
WORKDIR "/src/../Wist.Server.Portal"
RUN dotnet build "Wist.Server.Portal.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Wist.Server.Portal.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Wist.Server.Portal.dll"]
