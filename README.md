# RevitChecklist
Quality checklist app for Revit, storing all data as JSON in DataStorage elements.

This repository contains a minimal prototype with a C# `HttpListener` server and
a very simple client. The real implementation runs inside Revit and stores
checklist data using Revit's APIs.

## Building the server

```
cd src
dotnet build Server/ChecklistServer/ChecklistServer.csproj -c Release
```

Open `src/Client/index.html` in a browser to interact with the server while it
is running inside Revit.

## Building the Revit add-in

```
dotnet build src/Addin/ChecklistAddin.csproj -c Release
```

The add-in starts the local server when Revit loads and provides a ribbon button
named **Checklist** that opens your default browser to `http://localhost:51789/`.
