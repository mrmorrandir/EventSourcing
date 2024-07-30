using System.Text.RegularExpressions;

namespace EventSourcing.Mappers;

/// <summary>
/// <para>
/// The default event mapper will use the default <see cref="EventSerializer{TEvent}"/> and <see cref="EventDeserializer{TEvent}"/> to serialize and deserialize events.
/// The type name of the <see cref="TEvent"/> parameter will be converted to kebab case and used as the 'type' parameter for <see cref="AbstractEventMapper{TEvent}.Serialize"/> and <see cref="AbstractEventMapper{TEvent}.Deserialize"/>.
/// </para>
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public class DefaultEventMapper<TEvent> : AbstractEventMapper<TEvent> where TEvent : IEvent
{
    public DefaultEventMapper()
    {
        // Take the name of the event type which is pascal case and convert it to kebab case
        var type = typeof(TEvent).Name;
        var versionedKebabType = ToKebabCase(type);
        var unversionedKebabType = ToKebabCase(type, withVersion: false);
        WillSerialize(versionedKebabType);
        CanDeserialize(versionedKebabType);
        if (versionedKebabType != unversionedKebabType)
            CanDeserialize(unversionedKebabType);
    }

    private static string ToKebabCase(string type, bool withVersion = true)
    {
        var kebabCaseName = string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
        // Check if the kebab case name already has a version number with a regex
        if (!VersionSuffixRegex.IsMatch(kebabCaseName) && withVersion)
            kebabCaseName += "-v1"; // default versioning
        return kebabCaseName;
    }
}