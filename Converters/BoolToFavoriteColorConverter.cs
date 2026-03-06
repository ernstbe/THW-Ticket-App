using System.Globalization;

namespace THWTicketApp.Converters;

public class BoolToFavoriteColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
            return Color.FromArgb("#FF9800"); // Orange when active
        return Color.FromArgb("#9E9E9E"); // Gray when inactive
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
