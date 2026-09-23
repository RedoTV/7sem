// Лабораторная работа №4, вариант 7.
// Сумма ряда S = x + x^2/2 + x^3/3 + ... = sum(x^n/n) с точностью eps.
// Ряд сходится при -1 <= x < 1 и равен -ln(1-x) - это значение берется
// как контрольное при тестировании.
// Без аргументов - интерактивный расчет и график частичных сумм,
// аргумент "тест" - прогон тестов.

using System.Globalization;
using Avalonia;

namespace Lab4;

public static class Program
{
    // при x, близких к 1, ряд сходится слишком медленно, чтобы считать без ограничения
    private const int MaxTerms = 50_000_000;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Main(string[] args)
    {
        if (args.Any(a => a == "тест" || a == "test" || a == "--test"))
            RunTests();
        else
            Interactive();
    }

    private static void Interactive()
    {
        Console.WriteLine("Лабораторная работа №4, вариант 7. Сумма ряда S = x + x^2/2 + x^3/3 + ... = -ln(1-x)");
        Console.WriteLine("Область сходимости: -1 <= x < 1. Суммирование ведется, пока |x^n/n| >= eps.\n");

        while (true)
        {
            string? sx = Read("Введите x");
            if (sx == null) return;
            if (ParseX(sx, out double x) is string ex) { Console.WriteLine("Ошибка: " + ex + "\n"); continue; }

            string? se = Read("Введите eps");
            if (se == null) return;
            if (ParseEps(se, out double eps) is string ee) { Console.WriteLine("Ошибка: " + ee + "\n"); continue; }

            var (sum, terms, error) = SumSeries(x, eps, MaxTerms);
            if (error != null) { Console.WriteLine("Отказ: " + error + "\n"); continue; }

            double exact = -Math.Log(1 - x);
            Console.WriteLine($"\nS = {sum.ToString("F10", Inv)}  ({terms} членов ряда)");
            Console.WriteLine("Контрольное значение -ln(1-x) = " + exact.ToString("F10", Inv));
            Console.WriteLine("Фактическая погрешность = " + Math.Abs(sum - exact).ToString("E3", Inv));

            string? answer = Read("Показать график частичных сумм? (д/н)");
            if (answer != null && answer.Trim().ToLowerInvariant().StartsWith("д"))
                ShowPlot(x, eps, sum, terms, exact);
            Console.WriteLine();
        }
    }

    private static string? Read(string prompt)
    {
        Console.Write(prompt + ": ");
        return Console.ReadLine();
    }

    private static void ShowPlot(double x, double eps, double sum, int terms, double exact)
    {
        // ряд считается второй раз: первый проход дал только сумму, для графика нужны все частичные суммы
        var trace = new List<(int N, double S)>();
        if (terms > 0) SumSeries(x, eps, MaxTerms, trace);

        AppBuilder.Configure(() => new PlotApp(x, eps, sum, terms, exact, trace))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(Array.Empty<string>());
    }

    // запятая и точка равнозначны; возврат - текст ошибки или null
    public static string? ParseX(string? text, out double x)
    {
        x = 0;
        string? error = ParseNumber(text, out x);
        if (error != null) return error;

        if (double.IsNaN(x) || x < -1 || x >= 1)
            return "ряд расходится при |x| >= 1, допустимы значения из [-1; 1)";
        return null;
    }

    public static string? ParseEps(string? text, out double eps)
    {
        eps = 0;
        string? error = ParseNumber(text, out eps);
        if (error != null) return error;

        if (double.IsNaN(eps) || eps <= 0)
            return "eps должна быть конечным положительным числом";
        return null;
    }

    private static string? ParseNumber(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return "пустой ввод";

        if (!double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, Inv, out value))
            return $"\"{text.Trim()}\" - не число";
        if (double.IsInfinity(value)) return "число слишком велико";
        return null;
    }

    // a(n) = x^n/n считается из предыдущего члена, без возведения в степень.
    // trace, если передан, собирает точки (n, S(n)) для графика.
    public static (double Sum, int Terms, string? Error) SumSeries(
        double x, double eps, int maxTerms, List<(int N, double S)>? trace = null)
    {
        if (double.IsNaN(x) || double.IsInfinity(x) || x < -1 || x >= 1)
            return (0, 0, "ряд расходится при |x| >= 1, допустимы значения из [-1; 1)");
        if (double.IsNaN(eps) || double.IsInfinity(eps) || eps <= 0)
            return (0, 0, "eps должна быть конечным положительным числом");

        double sum = 0, power = 1;
        int terms = 0;

        for (int n = 1; n <= maxTerms; n++)
        {
            power *= x;
            double term = power / n;
            if (Math.Abs(term) < eps) break;

            sum += term;
            terms = n;

            // все точки до 400-й и дальше каждая 400-я, иначе на графике миллион точек
            if (trace != null && (n <= 400 || n % 400 == 0)) trace.Add((n, sum));
        }

        if (terms == maxTerms)
            return (0, 0, $"за {maxTerms} членов ряда точность {eps} не достигнута (увеличьте eps)");

        if (trace != null && terms > 0 && (trace.Count == 0 || trace[^1].N < terms))
            trace.Add((terms, sum));

        return (sum, terms, null);
    }

    private static int _pass, _fail;

    private static void RunTests()
    {
        Console.WriteLine("Прогон тестов: нормальные условия, экстремальные условия, исключительные ситуации");
        Console.WriteLine("Эталонные значения просчитаны вручную или по контрольной формуле -ln(1-x).\n");

        // нормальные условия: характерные данные из области сходимости
        CheckSum("НОРМА ", "x=0,5  eps=0,001", 0.5, 0.001, null, 0.6922619048, 7, 1e-9, "вручную");
        CheckSum("НОРМА ", "x=-0,5 eps=0,001", -0.5, 0.001, null, -0.4058035714, 7, 1e-9, "вручную");
        CheckSum("НОРМА ", "x=0    eps=0,001", 0, 0.001, null, 0, 0, 0, "вырожденный ряд");
        CheckSum("НОРМА ", "x=0,9  eps=1e-6 ", 0.9, 1e-6, null, -Math.Log(0.1), -1, 2e-5, "-ln(0,1)");

        // экстремальные условия: границы области, большая и малая точность, медленная сходимость
        CheckSum("ЭКСТР ", "x=0,5  eps=0,4  ", 0.5, 0.4, null, 0.5, 1, 0, "успевает один член");
        CheckSum("ЭКСТР ", "x=0,5  eps=1e-13", 0.5, 1e-13, null, Math.Log(2), -1, 1e-12, "большая точность");
        CheckSum("ЭКСТР ", "x=0,99 eps=1e-6 ", 0.99, 1e-6, null, -Math.Log(0.01), -1, 5e-4, "медленная сходимость");
        CheckSum("ЭКСТР ", "x=-0,99 eps=1e-6", -0.99, 1e-6, null, -Math.Log(1.99), -1, 2e-6, "знакочередующийся");
        CheckSum("ЭКСТР ", "x=-1   eps=1e-6 ", -1, 1e-6, null, -Math.Log(2), -1, 2e-6, "граница области");

        // исключительные ситуации: данные вне области, мусор, защита от зацикливания
        CheckErr("ИСКЛЮЧ", "x=1", () => SumSeries(1, 0.001, MaxTerms).Error, "расходится");
        CheckErr("ИСКЛЮЧ", "x=1,5", () => SumSeries(1.5, 0.001, MaxTerms).Error, "расходится");
        CheckErr("ИСКЛЮЧ", "x=-1,5", () => SumSeries(-1.5, 0.001, MaxTerms).Error, "расходится");
        CheckErr("ИСКЛЮЧ", "x=NaN", () => SumSeries(double.NaN, 0.001, MaxTerms).Error, "");
        CheckErr("ИСКЛЮЧ", "eps=0", () => SumSeries(0.5, 0, MaxTerms).Error, "eps");
        CheckErr("ИСКЛЮЧ", "eps=-0,1", () => SumSeries(0.5, -0.1, MaxTerms).Error, "eps");
        CheckErr("ИСКЛЮЧ", "x=0,999999 eps=1e-9 лимит 500", () => SumSeries(0.999999, 1e-9, 500).Error, "не достигнута");
        CheckErr("ИСКЛЮЧ", "ввод 'abc' в поле x", () => ParseX("abc", out _), "не число");
        CheckErr("ИСКЛЮЧ", "пустой ввод", () => ParseX(" ", out _), "пустой");
        CheckErr("ИСКЛЮЧ", "ввод '1,5' в поле x", () => ParseX("1,5", out _), "расходится");
        CheckErr("ИСКЛЮЧ", "ввод '1e400' в поле eps", () => ParseEps("1e400", out _), "велико");
        CheckErr("ИСКЛЮЧ", "ввод 'abc' в поле eps", () => ParseEps("abc", out _), "не число");

        Console.WriteLine($"\nПройдено {_pass} из {_pass + _fail}.");
        if (_fail == 0)
            Console.WriteLine("Покрыты все ветви: штатный расчет, вырожденные случаи, все виды отказов.");
        else
            Console.WriteLine("ЕСТЬ ПРОВАЛЕННЫЕ ТЕСТЫ!");

        Environment.ExitCode = _fail == 0 ? 0 : 1;
    }

    private static void CheckSum(string kind, string input, double x, double eps, int? maxTerms,
        double expected, int expectedTerms, double tol, string note)
    {
        var (sum, terms, error) = SumSeries(x, eps, maxTerms ?? MaxTerms);
        bool ok = error == null && Math.Abs(sum - expected) <= tol
                  && (expectedTerms < 0 || terms == expectedTerms);

        string got = error != null ? "отказ: " + error
            : $"S={sum.ToString("F7", Inv)} ({terms} член.)";
        string want = expectedTerms < 0
            ? $"{expected.ToString("F7", Inv)} ±{tol.ToString("G2", Inv)}"
            : $"{expected.ToString("F7", Inv)}, N={expectedTerms}";

        Console.WriteLine($"{kind}| {input,-31}| {got,-44}| ожид. {want,-27}| {note,-20}| {(ok ? "ОК" : "ПРОВАЛ")}");
        if (ok) _pass++; else _fail++;
    }

    private static void CheckErr(string kind, string input, Func<string?> action, string fragment)
    {
        string? error = action();
        bool ok = error != null && (fragment.Length == 0 || error.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        string shown = error ?? "ОТКАЗА НЕТ - ЭТО ОШИБКА";
        if (shown.Length > 60) shown = shown[..59] + "…";

        Console.WriteLine($"{kind}| {input,-31}| {shown,-60}| {(ok ? "ОК" : "ПРОВАЛ")}");
        if (ok) _pass++; else _fail++;
    }
}
