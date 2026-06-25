FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY MafWorkflow.sln .
COPY src/ src/
COPY tests/ tests/

RUN dotnet restore
RUN dotnet build --no-restore -c Release

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/src/TicketTriage.Workflow/bin/Release/net8.0/ .

ENTRYPOINT ["dotnet", "TicketTriage.Workflow.dll"]