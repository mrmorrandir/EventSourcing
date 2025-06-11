using System.Reflection;
using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQ.DI;

public class RabbitMqPublisherOptionsBuilder
{
    private string _host = "localhost";
    private string _password = "guest";
    private string _username = "guest";
    private string _baseExchangeName = "events";

    public RabbitMqPublisherOptionsBuilder()
    {
    }

    public RabbitMqPublisherOptionsBuilder UseConnection(string host, string username, string password)
    {
        _host = host;
        _username = username;
        _password = password;
        return this;
    }
    
    public RabbitMqPublisherOptionsBuilder UseBaseExchangeName(string baseExchangeName)
    {
        _baseExchangeName = baseExchangeName;
        return this;
    }

    public RabbitMqPublisherOptions Build()
    {
        if (string.IsNullOrWhiteSpace(_host))
            throw new ArgumentException("Host cannot be null or empty.", nameof(_host));

        if (string.IsNullOrWhiteSpace(_username))
            throw new ArgumentException("Username cannot be null or empty.", nameof(_username));

        if (string.IsNullOrWhiteSpace(_password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(_password));

        if (string.IsNullOrWhiteSpace(_baseExchangeName))
            throw new ArgumentException("Base exchange name cannot be null or empty.", nameof(_baseExchangeName));
        
        return new RabbitMqPublisherOptions(_host, _username, _password, _baseExchangeName);
    }
}