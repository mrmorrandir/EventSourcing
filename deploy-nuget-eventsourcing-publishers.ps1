param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

& ./deploy-nuget-base.ps1 -ProjectPath "src/EventSourcing.Publishers/EventSourcing.Publishers.csproj" -ApiKey $ApiKey
