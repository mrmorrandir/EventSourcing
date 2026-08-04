namespace EventSourcing.SourceGenerators;

public static class PlainTextCommentSerializer
{
    public static string SerializeToComment(object obj, int indent = 0, HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        var sb = new System.Text.StringBuilder();
        string indentStr = new string(' ', indent * 2);

        if (obj == null)
        {
            sb.AppendLine($"{indentStr}// (null)");
            return sb.ToString();
        }

        if (!IsSimple(obj.GetType()))
        {
            if (!visited.Add(obj))
            {
                sb.AppendLine($"{indentStr}// (cycle detected)");
                return sb.ToString();
            }
        }

        var type = obj.GetType();
        if (IsEnumerable(type) && type != typeof(string))
        {
            sb.AppendLine($"{indentStr}// {type.Name}:");
            foreach (var item in (System.Collections.IEnumerable)obj)
            {
                sb.Append(SerializeToComment(item, indent + 1, visited));
            }
        }
        else if (!IsSimple(type))
        {
            sb.AppendLine($"{indentStr}// {type.Name}:");
            foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                object? value = null;
                try { value = prop.GetValue(obj); } catch { }
                if (IsSimple(prop.PropertyType) || value == null)
                {
                    sb.AppendLine($"{indentStr}  // {prop.Name}: {value ?? "(null)"}");
                }
                else
                {
                    sb.AppendLine($"{indentStr}  // {prop.Name}:");
                    sb.Append(SerializeToComment(value, indent + 2, visited));
                }
            }
        }
        else
        {
            sb.AppendLine($"{indentStr}// {obj}");
        }

        return sb.ToString();
    }

    private static bool IsSimple(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid);

    private static bool IsEnumerable(Type type) =>
        typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    // Reference equality comparer for cycle detection
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}