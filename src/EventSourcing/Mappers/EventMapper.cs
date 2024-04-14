using System.Text.RegularExpressions;

namespace EventSourcing.Mappers;

public abstract class EventMapper
{
    protected static readonly Regex TypeRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*-v[0-9]+$", RegexOptions.Compiled);
    protected static readonly Regex VersionSuffixRegex = new(@"-v[0-9]+$", RegexOptions.Compiled);
}