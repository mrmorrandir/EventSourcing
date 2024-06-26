﻿namespace EventSourcing.Mappers;

public interface IEventRegistry 
{
    ISerializedEvent Serialize(IEvent @event);
    IEvent Deserialize(string type, string data);
}