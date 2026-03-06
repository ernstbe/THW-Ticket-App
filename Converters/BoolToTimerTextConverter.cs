using System.Globalization;

namespace THWTicketApp.Converters;

public class BoolToTimerTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Timer stoppen" : "Timer starten";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
