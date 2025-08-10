using System.Text.Json;

namespace StreamKey.Infrastructure.Extensions;

public static class ObjectExtensions
{
    public static bool IsSimpleType(this Type type)
    {
        return type.IsPrimitive 
               || type.IsEnum 
               || type == typeof(string) 
               || type == typeof(decimal) 
               || type == typeof(DateTime) 
               || type == typeof(DateTimeOffset) 
               || type == typeof(TimeSpan) 
               || type == typeof(Guid);
    }
    
    public static object? DeserializeValue(this string value)
    {
        if (bool.TryParse(value, out var boolValue)) return boolValue;
            
        if (int.TryParse(value, out var intValue)) return intValue;
            
        if (long.TryParse(value, out var longValue)) return longValue;
            
        if (double.TryParse(value, out var doubleValue)) return doubleValue;
            
        if (decimal.TryParse(value, out var decimalValue)) return decimalValue;
            
        if (DateTime.TryParse(value, out var dateTimeValue)) return dateTimeValue;
            
        if (Guid.TryParse(value, out var guidValue)) return guidValue;

        if (!value.StartsWith('{') && !value.StartsWith('[')) return value;
        
        try
        {
            return JsonSerializer.Deserialize<object>(value);
        }
        catch
        {
            return value;
        }
    }
}