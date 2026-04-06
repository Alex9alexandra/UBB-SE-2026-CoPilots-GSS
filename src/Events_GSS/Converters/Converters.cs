using System;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Events_GSS.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && b)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string s)
            return string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;

        return value is not null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts bool IsMention → accent color for mentions, default for plain text.
/// </summary>
public class MentionForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isMention && isMention)
            return new SolidColorBrush(Colors.DodgerBlue);

        // Return null to use the default foreground from the theme
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts bool IsMention → SemiBold for mentions, Normal for plain text.
/// </summary>
public class MentionFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isMention && isMention)
            return FontWeights.SemiBold;

        return FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts bool IsExpanded to a chevron glyph: ▼ when expanded, ▶ when collapsed.
/// </summary>
public class ExpandChevronConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool expanded && expanded)
            return "\uE70D"; // ChevronDown
        return "\uE76C"; // ChevronRight
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}