// Графическая интерпретация результата: кривая частичных сумм S(n)
// и контрольная прямая -ln(1-x), к которой ряд должен сходиться.

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
    readonly double _x, _eps, _sum, _exact;
    readonly int _terms;
    readonly List<(int N, double S)> _trace;

    public PlotApp(double x, double eps, double sum, int terms, double exact, List<(int N, double S)> trace)
    {
        _x = x; _eps = eps; _sum = sum; _terms = terms; _exact = exact; _trace = trace;
    }

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted() =>
        new PlotWindow(_x, _eps, _sum, _terms, _exact, _trace).Show();
}

public class PlotWindow : Window
{
    // поле графика в координатах окна
    const double L = 70, R = 870, T = 45, B = 500;

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    readonly Canvas _canvas = new();

    public PlotWindow(double x, double eps, double sum, int terms, double exact, List<(int N, double S)> trace)
    {
        Title = $"Сумма ряда: x = {F(x)}, eps = {F(eps)}, S = {F(sum)}, членов: {terms}";
        Width = 900;
        Height = 560;
        Content = _canvas;

        if (terms == 0)
        {
            AddText($"S = 0: уже первый член ряда меньше eps = {F(eps)}, график не строится",
                15, FontWeight.SemiBold, Brushes.Black, 30, 30);
            return;
        }

        var pts = new List<(double N, double S)> { (0, 0) };
        pts.AddRange(trace.Select(p => ((double)p.N, p.S)));

        double ymin = 0, ymax = 0;
        foreach (var p in pts) { ymin = Math.Min(ymin, p.S); ymax = Math.Max(ymax, p.S); }
        ymin = Math.Min(ymin, exact);
        ymax = Math.Max(ymax, exact);
        double pad = (ymax - ymin) * 0.08;
        if (pad == 0) pad = Math.Max(0.5, Math.Abs(exact) * 0.1);
        ymin -= pad;
        ymax += pad;

        double Y(double s) => B - (s - ymin) / (ymax - ymin) * (B - T);
        double Xn(double n) => L + n / terms * (R - L);

        // сетка и подписи по вертикали
        foreach (double v in new[] { ymin, (ymin + ymax) / 2, ymax })
        {
            _canvas.Children.Add(new Line
            {
                StartPoint = new Point(L, Y(v)), EndPoint = new Point(R, Y(v)),
                Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224))
            });
            AddText(v.ToString("F3", Inv), 12, FontWeight.Normal, Brushes.Black, L - 66, Y(v) - 8, width: 58, alignRight: true);
        }

        // оси
        _canvas.Children.Add(new Line { StartPoint = new Point(L, T), EndPoint = new Point(L, B), Stroke = Brushes.Black, StrokeThickness = 1.5 });
        _canvas.Children.Add(new Line { StartPoint = new Point(L, B), EndPoint = new Point(R, B), Stroke = Brushes.Black, StrokeThickness = 1.5 });

        // контрольная прямая -ln(1-x)
        _canvas.Children.Add(new Line
        {
            StartPoint = new Point(L, Y(exact)), EndPoint = new Point(R, Y(exact)),
            Stroke = new SolidColorBrush(Color.FromRgb(178, 34, 34)),
            StrokeThickness = 1.5,
            StrokeDashArray = new AvaloniaList<double> { 4, 2 }
        });
        AddText($"-ln(1-x) = {F(exact)}", 13, FontWeight.Normal, Brushes.Firebrick, R - 205, Math.Max(T, Y(exact) - 22));

        // кривая частичных сумм
        var curve = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(70, 130, 180)), StrokeThickness = 2 };
        foreach (var p in pts) curve.Points.Add(new Point(Xn(p.N), Y(p.S)));
        _canvas.Children.Add(curve);

        // подписи
        AddText($"Частичные суммы S(n) ряда x^n/n при x = {F(x)}, eps = {F(eps)};  S = {F(sum)} за {terms} членов",
            15, FontWeight.SemiBold, Brushes.Black, L, 12);
        AddText("n — номер члена ряда", 13, FontWeight.Normal, Brushes.Black, (L + R) / 2 - 70, B + 14);
        AddText(terms.ToString(Inv), 12, FontWeight.Normal, Brushes.Black, R - 25, B + 14);
        AddText("0", 12, FontWeight.Normal, Brushes.Black, L - 4, B + 14);
        AddText("S(n)", 13, FontWeight.Normal, Brushes.Black, 18, T - 8);
    }

    void AddText(string text, double size, FontWeight weight, IBrush color,
        double left, double top, double width = 0, bool alignRight = false)
    {
        var tb = new TextBlock { Text = text, FontSize = size, FontWeight = weight, Foreground = color };
        if (width > 0)
        {
            tb.Width = width;
            tb.TextAlignment = alignRight ? TextAlignment.Right : TextAlignment.Left;
        }

        _canvas.Children.Add(tb);
        Canvas.SetLeft(tb, left);
        Canvas.SetTop(tb, top);
    }

    static string F(double v) => v.ToString("G6", Inv);
}
