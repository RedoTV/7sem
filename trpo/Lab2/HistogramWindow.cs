// Окно с гистограммой: диапазон значений делится на равные интервалы,
// высота столбика пропорциональна количеству попаданий в интервал.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Lab2;

// Точка запуска графической части: показывает единственное окно.
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
        RequestedThemeVariant = ThemeVariant.Light; // светлый фон окна независимо от темы системы
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
        Title = "Гистограмма";
        Width = 840;
        Height = 470;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        double min = values.Min();
        double intervalWidth = (values.Max() - min) / intervalCount;

        // подсчет попаданий в каждый интервал
        var counts = new int[intervalCount];

        foreach (double x in values)
        {
            int index = (int)((x - min) / intervalWidth);

            if (index == intervalCount) // значение, равное максимуму, относится к последнему интервалу
                index--;

            counts[index]++;
        }

        Content = BuildHistogram(counts, min, intervalWidth);
    }

    private Grid BuildHistogram(int[] counts, double min, double intervalWidth)
    {
        var grid = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        int maxCount = counts.Max();

        for (int i = 0; i < counts.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            int barHeight = (int)Math.Round((double)counts[i] / maxCount * MaxBarHeight);

            var column = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };

            column.Children.Add(new TextBlock
            {
                Text = counts[i].ToString(),
                TextAlignment = TextAlignment.Center,
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

            double from = min + i * intervalWidth;
            double to = from + intervalWidth;

            var range = new TextBlock
            {
                Text = $"[{from:F2}; {to:F2})",
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
