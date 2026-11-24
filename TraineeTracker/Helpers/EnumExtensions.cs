// Helpers/EnumExtensions.cs
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TraineeTracker.Helpers;

public static class EnumExtensions
{
    public static string DisplayName(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<DisplayAttribute>();
        return attr?.Name ?? value.ToString();
    }
}