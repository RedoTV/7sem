// Лабораторная работа №2, вариант 7.
// 100 случайных чисел с экспоненциальным распределением (lambda = 0,5),
// сортировка по возрастанию, разности соседних элементов Xi - X(i-1),
// среднее значение и дисперсия. Числа и характеристики выводятся
// в консоль, гистограмма из 10 интервалов - в отдельном окне.

using Avalonia;

namespace Lab2;

public static class Program
{
    private const int Count = 100;        // количество генерируемых чисел
    private const double Lambda = 0.5;    // параметр экспоненциального распределения
    private const int IntervalCount = 10; // число интервалов гистограммы

    public static void Main(string[] args)
    {
        int? seed = ReadSeed(); // null - генератор инициализируется случайно

        double[] generated = GenerateExponential(Count, Lambda, seed);
        Array.Sort(generated);

        Console.WriteLine($"Последовательность из {Count} чисел, экспоненциальный закон (lambda = {Lambda}):");
        PrintRow(generated);

        double[] differences = ComputeDifferences(generated);
        Console.WriteLine($"\nРазности соседних элементов Xi - X(i-1), количество = {differences.Length}:");
        PrintRow(differences);

        Console.WriteLine($"\nСреднее значение: {Mean(differences):F4}");
        Console.WriteLine($"Дисперсия:        {Variance(differences):F4}");

        // запускаем окно с гистограммой, программа завершится при его закрытии
        AppBuilder.Configure(() => new HistogramApp(differences, IntervalCount))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }

    // пустой ввод - случайный seed, нечисловой ввод повторяет запрос
    private static int? ReadSeed()
    {
        while (true)
        {
            Console.Write("Введите seed генератора (целое число, Enter - случайный): ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (int.TryParse(input, out int seed))
                return seed;

            Console.WriteLine("Ошибка: нужно целое число. Повторите ввод.");
        }
    }

    // z = -ln(u) / lambda, где u равномерно в (0; 1];
    // u = 1 - NextDouble() исключает u = 0 и несуществующий ln(0)
    private static double[] GenerateExponential(int count, double lambda, int? seed)
    {
        Random random = seed.HasValue ? new Random(seed.Value) : Random.Shared;

        var values = new double[count];

        for (int i = 0; i < count; i++)
            values[i] = -Math.Log(1.0 - random.NextDouble()) / lambda;

        return values;
    }

    private static double[] ComputeDifferences(double[] sorted)
    {
        var differences = new double[sorted.Length - 1];

        for (int i = 1; i < sorted.Length; i++)
            differences[i - 1] = sorted[i] - sorted[i - 1];

        return differences;
    }

    // mx = (1/k) * SUM(xi)
    private static double Mean(double[] values)
    {
        double sum = 0;

        foreach (double x in values)
            sum += x;

        return sum / values.Length;
    }

    // dx = (1/k) * SUM(xi^2) - mx^2
    private static double Variance(double[] values)
    {
        double sumOfSquares = 0;

        foreach (double x in values)
            sumOfSquares += x * x;

        return sumOfSquares / values.Length - Math.Pow(Mean(values), 2);
    }

    // печать массива по пять чисел в строке
    private static void PrintRow(double[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Console.Write($"{values[i],9:F3}");

            if ((i + 1) % 5 == 0 || i == values.Length - 1)
                Console.WriteLine();
        }
    }
}
