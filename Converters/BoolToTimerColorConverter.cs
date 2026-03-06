using System.Globalization;

namespace THWTicketApp.Converters;

public class BoolToTimerColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#E74C3C") : Color.FromArgb("#4CAF50");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
