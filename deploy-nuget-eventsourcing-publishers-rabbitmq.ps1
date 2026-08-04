param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

& ./deploy-nuget-base.ps1 -ProjectPath "src/EventSourcing.Publishers.RabbitMQ/EventSourcing.Publishers.RabbitMQ.csproj" -ApiKey $ApiKey
