namespace EventSourcing.Publishers.RabbitMQ.DI;

public record RabbitMqPublisherOptions(string Host, string Username, string Password, string BaseExchangeName);