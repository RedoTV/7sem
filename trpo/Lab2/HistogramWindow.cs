// Окно гистограммы. Диапазон делится на равные интервалы,
// высота столбика пропорциональна числу попаданий.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Lab2;

public class HistogramApp : Application
{
    private readonly double[] _values;
    private readonly int _intervalCount;

    public HistogramApp(double[] values, int intervalCount)
    {
        _values = values;
        _intervalCount = intervalCount;
    }

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        new HistogramWindow(_values, _intervalCount).Show();
    }
}

public class HistogramWindow : Window
{
    private const int MaxBarHeight = 280;
    private const int BarWidth = 46;

    public HistogramWindow(double[] values, int intervalCount)
    {
        Title = "Гистограмма разностей";
        Width = 840;
        Height = 470;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        double min = values.Min();
        double max = values.Max();
        double width = (max - min) / intervalCount;
        int[] counts = CountIntervals(values, min, width, intervalCount);

        Content = BuildHistogram(counts, min, width);
    }

    // width == 0, когда все разности совпали: делить диапазон не на что
    private static int[] CountIntervals(double[] values, double min, double width, int intervalCount)
    {
        var counts = new int[intervalCount];

        if (width == 0)
        {
            counts[0] = values.Length;
            return counts;
        }

        foreach (double x in values)
        {
            int index = (int)((x - min) / width);

            // из-за округления максимум иногда оказывается ровно на границе
            if (index >= intervalCount)
                index = intervalCount - 1;

            counts[index]++;
        }

        return counts;
    }

    private Grid BuildHistogram(int[] counts, double min, double width)
    {
        var grid = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        int maxCount = counts.Max();
        if (maxCount == 0)
            maxCount = 1;

        for (int i = 0; i < counts.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            int barHeight = (int)Math.Round((double)counts[i] / maxCount * MaxBarHeight);
            var column = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };

            column.Children.Add(new TextBlock
            {
                Text = counts[i].ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            column.Children.Add(new Rectangle
            {
                Width = BarWidth,
                Height = barHeight,
                Fill = Brushes.SteelBlue,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            Grid.SetColumn(column, i);
            grid.Children.Add(column);

            double from = min + i * width;
            var range = new TextBlock
            {
                Text = $"[{from:F2}; {from + width:F2})",
                FontSize = 11,
                TextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(range, i);
            Grid.SetRow(range, 1);
            grid.Children.Add(range);
        }

        return grid;
    }
}
