param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

& ./deploy-nuget-base.ps1 -ProjectPath "src/EventSourcing.Abstractions/EventSourcing.Abstractions.csproj" -ApiKey $ApiKey
