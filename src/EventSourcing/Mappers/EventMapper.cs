using System.Text.RegularExpressions;

namespace EventSourcing.Mappers;

/// <summary>
/// <para>
/// Non-Generic base class for AbstractEventMapper.
/// </para>
/// <para>
/// https://www.jetbrains.com/help/rider/StaticMemberInGenericType.html?utm_source=product
/// </para>
/// <para>
/// In the vast majority of cases, having a static field or auto-property in a generic type is a sign of an error. The reason for this is that a static member in a generic type will not be shared among instances of different close constructed types. This means that for a generic class MyGeneric&lt;T&gt; which has public static string MyProp { get; set; }, the values of MyGeneric&lt;int&gt;.MyProp and MyGeneric&lt;string&gt;.MyProp have completely different, independent values.
/// </para>
/// <para>
/// If you need to have a static field shared between instances with different generic arguments, define a non-generic base class to store your static members, then set your generic type to inherit from this type.
/// </para>
/// </summary>
public abstract class EventMapper
{
    protected static readonly Regex TypeRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*-v[0-9]+$", RegexOptions.Compiled);
    protected static readonly Regex VersionSuffixRegex = new(@"-v[0-9]+$", RegexOptions.Compiled);
}