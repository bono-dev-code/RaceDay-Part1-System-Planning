FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY part3/RaceDay.Mvc/RaceDay.Mvc.csproj part3/RaceDay.Mvc/
RUN dotnet restore part3/RaceDay.Mvc/RaceDay.Mvc.csproj
COPY part3/RaceDay.Mvc/ part3/RaceDay.Mvc/
RUN dotnet publish part3/RaceDay.Mvc/RaceDay.Mvc.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
