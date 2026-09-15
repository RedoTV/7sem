// Лабораторная работа №4, вариант 7. Отладка и тестирование ПС.
// Вычисляется сумма ряда S = x + x^2/2 + x^3/3 + ... = sum(x^n/n), n = 1..inf, с точностью eps:
// суммирование ведется, пока очередной член по модулю не станет меньше eps.
// Ряд сходится при -1 <= x < 1, его сумма равна -ln(1-x) - это контрольное значение для тестов.
// Запуск: без аргументов - интерактивный расчет и график частичных сумм;
//         аргумент "тест" - автоматический прогон тестов (норма / экстремал / исключ).

using System.Globalization;
using Avalonia;

namespace Lab4;

public static class Program
{
    // защита от зацикливания: при x, близких к 1, сходимость очень медленная
    public const int MaxTerms = 50_000_000;

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Main(string[] args)
    {
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* вывод перенаправлен */ }

        if (args.Any(a => a == "тест" || a == "test" || a == "--test"))
            RunTests();
        else
            Interactive();
    }

    // ---------------- интерактивный режим ----------------

    static void Interactive()
    {
        Console.WriteLine("Лабораторная работа №4, вариант 7. Сумма ряда S = x + x^2/2 + x^3/3 + ... = -ln(1-x)");
        Console.WriteLine("Область сходимости: -1 <= x < 1. Точность: суммирование ведется, пока |x^n/n| >= eps.\n");

        while (true)
        {
            string? sx = Read("Введите x (пустая строка — выход)");
            if (sx == null) return;
            if (ParseX(sx, out double x) is string ex) { Console.WriteLine("Ошибка: " + ex + "\n"); continue; }

            string? se = Read("Введите eps");
            if (se == null) return;
            if (ParseEps(se, out double eps) is string ee) { Console.WriteLine("Ошибка: " + ee + "\n"); continue; }

            var (sum, terms, error) = SumSeries(x, eps, MaxTerms);
            if (error != null) { Console.WriteLine("Отказ: " + error + "\n"); continue; }

            double exact = -Math.Log(1 - x);
            Console.WriteLine($"\nS = {sum.ToString("F10", Inv)}  ({terms} членов ряда)");
            if (terms == 0) Console.WriteLine("(первый член ряда уже меньше eps)");
            Console.WriteLine("Контрольное значение -ln(1-x) = " + exact.ToString("F10", Inv));
            Console.WriteLine("Фактическая погрешность = " + Math.Abs(sum - exact).ToString("E3", Inv));

            string? answer = Read("Показать график частичных сумм? (д/н)");
            if (answer != null && answer.Trim().ToLowerInvariant().StartsWith("д"))
                ShowPlot(x, eps, sum, terms, exact);
            Console.WriteLine();
        }
    }

    static string? Read(string prompt)
    {
        Console.Write(prompt + ": ");
        return Console.ReadLine();
    }

    static void ShowPlot(double x, double eps, double sum, int terms, double exact)
    {
        var trace = new List<(int N, double S)>();
        if (terms > 0) SumSeries(x, eps, MaxTerms, trace); // повторный проход - собираем точки графика

        AppBuilder.Configure(() => new PlotApp(x, eps, sum, terms, exact, trace))
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(Array.Empty<string>());
    }

    // ---------------- вычислительное ядро (его и тестируем) ----------------

    // разбор числа: запятая и точка равнозначны; возврат - текст ошибки или null
    public static string? ParseDouble(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return "пустой ввод";

        string s = text.Trim().Replace(',', '.');
        if (!double.TryParse(s, NumberStyles.Float, Inv, out value))
            return $"\"{text.Trim()}\" - не число";
        if (double.IsInfinity(value)) return "число слишком велико";
        return null;
    }

    public static string? ParseX(string? text, out double x)
    {
        x = 0;
        string? e = ParseDouble(text, out x);
        if (e != null) return e;

        if (double.IsNaN(x) || x < -1 || x >= 1)
            return "ряд расходится при |x| >= 1, допустимы значения из [-1; 1)";
        return null;
    }

    public static string? ParseEps(string? text, out double eps)
    {
        eps = 0;
        string? e = ParseDouble(text, out eps);
        if (e != null) return e;

        if (double.IsNaN(eps) || eps <= 0)
            return "eps должна быть конечным положительным числом";
        return null;
    }

    // частичные суммы: член a(n) = x^n/n пересчитывается из предыдущего, без возведения в степень;
    // trace (если не null) накапливает точки (n, S_n) для графика
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
            power *= x;          // power = x^n
            double term = power / n; // член a(n) = x^n/n
            if (Math.Abs(term) < eps) break;

            sum += term;
            terms = n;
            if (trace != null && (n <= 400 || n % 400 == 0)) trace.Add((n, sum));
        }

        // цикл доел до конца, критерий останова так и не сработал
        if (terms == maxTerms)
            return (0, 0, $"за {maxTerms} членов ряда точность {eps} не достигнута (увеличьте eps)");

        if (trace != null && terms > 0 && (trace.Count == 0 || trace[^1].N < terms))
            trace.Add((terms, sum));

        return (sum, terms, null);
    }

    // ---------------- тесты ----------------

    static int _pass, _fail;

    static void RunTests()
    {
        Console.WriteLine("Прогон тестов: нормальные условия, экстремальные условия, исключительные ситуации");
        Console.WriteLine("Эталонные значения просчитаны вручную или по контрольной формуле -ln(1-x).\n");

        // нормальные условия: характерные данные из области сходимости
        CheckSum("НОРМА ", "x=0,5  eps=0,001", 0.5, 0.001, null, 0.6922619048, 7, 1e-9, "вручную");
        CheckSum("НОРМА ", "x=-0,5 eps=0,001", -0.5, 0.001, null, -0.4058035714, 7, 1e-9, "вручную");
        CheckSum("НОРМА ", "x=0    eps=0,001", 0, 0.001, null, 0, 0, 0, "вырожденный ряд");
        CheckSum("НОРМА ", "x=0,9  eps=1e-6 ", 0.9, 1e-6, null, -Math.Log(0.1), -1, 2e-5, "-ln(0,1)");

        // экстремальные: границы области, большая/малая точность, медленная сходимость
        CheckSum("ЭКСТР ", "x=0,5  eps=0,4  ", 0.5, 0.4, null, 0.5, 1, 0, "успевает один член");
        CheckSum("ЭКСТР ", "x=0,5  eps=1e-13", 0.5, 1e-13, null, Math.Log(2), -1, 1e-12, "большая точность");
        CheckSum("ЭКСТР ", "x=0,99 eps=1e-6 ", 0.99, 1e-6, null, -Math.Log(0.01), -1, 5e-4, "медленная сходимость");
        CheckSum("ЭКСТР ", "x=-0,99 eps=1e-6", -0.99, 1e-6, null, -Math.Log(1.99), -1, 2e-6, "знакочередующийся");
        CheckSum("ЭКСТР ", "x=-1   eps=1e-6 ", -1, 1e-6, null, -Math.Log(2), -1, 2e-6, "граница области");

        // исключительные: данные вне области, мусор, защита от зацикливания
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
        Console.WriteLine(_fail == 0
            ? "Покрыты все ветви: штатный расчет, вырожденные случаи, все виды отказов."
            : "ЕСТЬ ПРОВАЛЕННЫЕ ТЕСТЫ!");
        Environment.ExitCode = _fail == 0 ? 0 : 1;
    }

    static void CheckSum(string kind, string input, double x, double eps, int? maxTerms,
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
        Count(ok);
    }

    static void CheckErr(string kind, string input, Func<string?> action, string fragment)
    {
        string? error = action();
        bool ok = error != null && (fragment.Length == 0 || error.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"{kind}| {input,-31}| {Trunc(error ?? "ОТКАЗА НЕТ - ЭТО ОШИБКА", 60),-60}| {(ok ? "ОК" : "ПРОВАЛ")}");
        Count(ok);
    }

    static void Count(bool ok)
    {
        if (ok) _pass++; else _fail++;
    }

    static string Trunc(string s, int len) => s.Length <= len ? s : s[..(len - 1)] + "…";
}
