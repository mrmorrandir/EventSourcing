param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

& ./deploy-nuget-base.ps1 -ProjectPath "src/EventSourcing.SourceGenerators/EventSourcing.SourceGenerators.csproj" -ApiKey $ApiKey
