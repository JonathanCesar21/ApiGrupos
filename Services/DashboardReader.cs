using System.Data.Common;
using System.Globalization;
using System.Reflection;

namespace ApiGrupos.Services;

public static class DashboardReader
{
    // A projecao SQL e explicita e cada coluna precisa existir no DTO: evita campos
    // adicionados no SELECT sem mapeamento e preserva NULL (inclusive bool false).
    public static T Read<T>(DbDataReader reader) where T : new()
    {
        var result = new T();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!Properties<T>.Writable.TryGetValue(reader.GetName(i), out var property))
                throw new InvalidOperationException($"Coluna sem mapeamento em {typeof(T).Name}: {reader.GetName(i)}.");
            if (reader.IsDBNull(i))
            {
                if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null)
                    throw new InvalidOperationException($"NULL inesperado em {typeof(T).Name}.{property.Name}.");
                property.SetValue(result, null);
                continue;
            }
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var value = reader.GetValue(i);
            property.SetValue(result, targetType == typeof(DateTime)
                ? DateTime.SpecifyKind(Convert.ToDateTime(value, CultureInfo.InvariantCulture), DateTimeKind.Unspecified)
                : Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));
        }
        return result;
    }

    private static class Properties<T>
    {
        internal static readonly Dictionary<string, PropertyInfo> Writable = typeof(T).GetProperties()
            .Where(p => p.CanWrite).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }
}
