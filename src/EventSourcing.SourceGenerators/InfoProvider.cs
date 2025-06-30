namespace EventSourcing.SourceGenerators;

public class InfoProvider
{
    
}

public class Infos
{
    private class AggregateInfo
    {
        public string AggregateNamespace = "";
        public string AggregateName = "";
        public string AggregateFullName = "";
        public List<MutateMethodInfo> ApplyMethods = new();
        public List<MutateMethodInfo> CreateMethods = new();
        public string RepositoryNamespace = "";
        public string RepositoryName = "";
        public string RepositoryFullName = "";
        public string StateRepositoryName = "";
        public string StateRepositoryNamespace = "";
        public string StateRepositoryFullName = "";
        public bool CreateStateRepository = false;
    }
    
    private class RepositoryInfo // aka AggregateInfo (nearly identical)
    {
        public string AggregateNamespace = "";
        public string AggregateName = "";
        public string AggregateFullName = "";
        public string RepositoryNamespace = "";
        public string RepositoryName = "";
        public string RepositoryFullName = "";
        public string StateRepositoryName = "";
        public string StateRepositoryNamespace = "";
        public string StateRepositoryFullName = "";
        public bool CreateStateRepository = false;
    }
    
    private class MutateMethodInfo
    {
        public string MethodName = "";
        public string MethodFullName = "";
        public string EventNamespace = "";
        public string EventName = "";
        public string EventFullName = "";
    }

    public class MapperInfo
    {
        public string EventFullName = "";
        public string EventSchemaName = "";
        public string EventName = "";
        public string EventNamespace = "";
        public string EventVariableName = "";
        public string EventFieldName = "";
        public string MapperFieldName = "";
        public string MapperFullname = "";
        public string MapperName = "";
        public string MapperNamespace = "";
    }

    public class EventInfo // aka ProjectorInfo
    {
        public string EventName = "";
        public string EventFullName = "";
        public string EventNamespace = "";
        public string EventKebabCaseName = "";
        public string AggregateFullName = "";
        public bool IsCreateEvent = false;
    }
    
    
    
    
}