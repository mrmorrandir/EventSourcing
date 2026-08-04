namespace EventSourcing.Repositories;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UseStateRepositoryAttribute : Attribute
{
    public bool SaveStateResult { get; }

    public UseStateRepositoryAttribute(bool saveStateResult)
    {
        SaveStateResult = saveStateResult;
    }
}