// Окно с графиком частичных сумм S(n). Пунктиром - контрольное
// значение -ln(1-x), к которому ряд должен сходиться.

using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Lab4;

public class PlotApp : Application
{
    private readonly double _x, _eps, _sum, _exact;
    private readonly int _terms;
    private readonly List<(int N, double S)> _trace;

    public PlotApp(double x, double eps, double sum, int terms, double exact, List<(int N, double S)> trace)
    {
        _x = x; _eps = eps; _sum = sum; _terms = terms; _exact = exact; _trace = trace;
    }

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        new PlotWindow(_x, _eps, _sum, _terms, _exact, _trace).Show();
    }
}

public class PlotWindow : Window
{
    private const double Left = 70, Right = 870, Top = 45, Bottom = 500;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly Canvas _canvas = new();

    public PlotWindow(double x, double eps, double sum, int terms, double exact, List<(int N, double S)> trace)
    {
        Title = $"Сумма ряда: x = {F(x)}, eps = {F(eps)}, S = {F(sum)}, членов: {terms}";
        Width = 900;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = _canvas;

        if (terms == 0)
        {
            AddText($"S = 0: уже первый член ряда меньше eps = {F(eps)}, график не строится",
                15, FontWeight.SemiBold, Brushes.Black, 30, 30);
            return;
        }

        var points = new List<(double N, double S)> { (0, 0) };
        points.AddRange(trace.Select(p => ((double)p.N, p.S)));

        double ymin = Math.Min(0, exact), ymax = Math.Max(0, exact);
        foreach (var p in points)
        {
            ymin = Math.Min(ymin, p.S);
            ymax = Math.Max(ymax, p.S);
        }

        double pad = (ymax - ymin) * 0.08;
        if (pad == 0) pad = Math.Max(0.5, Math.Abs(exact) * 0.1);
        ymin -= pad;
        ymax += pad;

        double Y(double s) => Bottom - (s - ymin) / (ymax - ymin) * (Bottom - Top);
        double Xn(double n) => Left + n / terms * (Right - Left);

        foreach (double v in new[] { ymin, (ymin + ymax) / 2, ymax })
        {
            _canvas.Children.Add(new Line
            {
                StartPoint = new Point(Left, Y(v)), EndPoint = new Point(Right, Y(v)),
                Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224))
            });
            AddText(v.ToString("F3", Inv), 12, FontWeight.Normal, Brushes.Black,
                Left - 66, Y(v) - 8, width: 58, alignRight: true);
        }

        _canvas.Children.Add(new Line
        {
            StartPoint = new Point(Left, Top), EndPoint = new Point(Left, Bottom),
            Stroke = Brushes.Black, StrokeThickness = 1.5
        });
        _canvas.Children.Add(new Line
        {
            StartPoint = new Point(Left, Bottom), EndPoint = new Point(Right, Bottom),
            Stroke = Brushes.Black, StrokeThickness = 1.5
        });

        _canvas.Children.Add(new Line
        {
            StartPoint = new Point(Left, Y(exact)), EndPoint = new Point(Right, Y(exact)),
            Stroke = new SolidColorBrush(Color.FromRgb(178, 34, 34)),
            StrokeThickness = 1.5,
            StrokeDashArray = new AvaloniaList<double> { 4, 2 }
        });
        AddText($"-ln(1-x) = {F(exact)}", 13, FontWeight.Normal, Brushes.Firebrick,
            Right - 205, Math.Max(Top, Y(exact) - 22));

        var curve = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
            StrokeThickness = 2
        };
        foreach (var p in points) curve.Points.Add(new Point(Xn(p.N), Y(p.S)));
        _canvas.Children.Add(curve);

        AddText($"Частичные суммы S(n) ряда x^n/n при x = {F(x)}, eps = {F(eps)};  S = {F(sum)} за {terms} членов",
            15, FontWeight.SemiBold, Brushes.Black, Left, 12);
        AddText("n - номер члена ряда", 13, FontWeight.Normal, Brushes.Black, (Left + Right) / 2 - 70, Bottom + 14);
        AddText(terms.ToString(Inv), 12, FontWeight.Normal, Brushes.Black, Right - 25, Bottom + 14);
        AddText("0", 12, FontWeight.Normal, Brushes.Black, Left - 4, Bottom + 14);
        AddText("S(n)", 13, FontWeight.Normal, Brushes.Black, 18, Top - 8);
    }

    private void AddText(string text, double size, FontWeight weight, IBrush color,
        double left, double top, double width = 0, bool alignRight = false)
    {
        var block = new TextBlock { Text = text, FontSize = size, FontWeight = weight, Foreground = color };
        if (width > 0)
        {
            block.Width = width;
            block.TextAlignment = alignRight ? TextAlignment.Right : TextAlignment.Left;
        }

        _canvas.Children.Add(block);
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, top);
    }

    private static string F(double v) => v.ToString("G6", Inv);
}
