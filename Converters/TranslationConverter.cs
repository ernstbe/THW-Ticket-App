using System.Globalization;
using THWTicketApp.Helpers;

namespace THWTicketApp.Converters;

/// <summary>
/// Converter zum Übersetzen von englischen Typ- und Prioritätsnamen ins Deutsche.
/// Verwendet den zentralen Translator.
/// </summary>
public class TranslationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return Translator.Translate(text);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
