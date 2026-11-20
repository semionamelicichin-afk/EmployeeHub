# Build and Publish

## Requirements for source builds

- Windows 10 or later, x64.
- .NET 10 SDK.
- NuGet access to the official packages declared in `src/EmployeeHub/EmployeeHub.csproj`.

Visual Studio can also be used with a version that supports .NET 10 and has the .NET desktop development workload installed. These are source-build requirements only. The self-contained published application does not require the SDK or a separately installed .NET runtime.

## Build

From the repository root:

```powershell
dotnet restore .\EmployeeHub.sln
dotnet build .\EmployeeHub.sln --configuration Release
```

## Publish a self-contained Windows x64 folder

```powershell
dotnet publish .\src\EmployeeHub\EmployeeHub.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  --output .\publish\win-x64
```

Copy `Start-EmployeeHub.cmd`, `Reset-Demo.cmd`, `README.md`, and the `docs` folder into the publish folder before zipping it. Keep the published runtime files together with `EmployeeHub.exe`; the folder is the runnable release. No Telerik credentials or external data service are needed. SQLite uses `winsqlite3.dll`, which is part of Windows 10 and later.

## Release contents

The Windows x64 ZIP should contain the complete publish folder, the launcher and reset scripts, and English documentation. The source ZIP should contain the solution, source project, scripts, and documentation but exclude `.git`, `bin`, `obj`, publish output, and temporary files.
