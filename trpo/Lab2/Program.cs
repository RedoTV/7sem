// Лабораторная работа №2, вариант 7.
//
// Что делает программа:
//   1. Генерирует 100 чисел с экспоненциальным распределением, lambda = 0,5.
//      z = -ln(u) / lambda, u берется из (0; 1], чтобы не получился ln(0).
//   2. Сортирует их по возрастанию.
//   3. Строит новую последовательность из разностей соседей: Xi - X(i-1).
//   4. Для разностей считает среднее и дисперсию и печатает гистограмму
//      на 10 интервалов (отдельное окно).
//
// mx = (1/k) * SUM(xi)
// dx = (1/k) * SUM(xi^2) - mx^2
//
// Оформление: у метода одно действие и имя по смыслу, ввод проверяется,
// комментарии стоят у формул и неочевидных условий.

using Avalonia;

namespace Lab2;

public static class Program
{
    private const int Count = 100;        // сколько чисел генерировать
    private const double Lambda = 0.5;    // параметр экспоненциального закона
    private const int IntervalCount = 10; // на сколько частей делить гистограмму

    public static void Main(string[] args)
    {
        int? seed = ReadSeed();

        double[] values = GenerateExponential(Count, Lambda, seed);

        for (int i = 0; i < values.Length-1; i++)
        {
            for (int j = 0; j < values.Length - i - 1 ; j++)
            {
                if (values[j] > values[j+1])
                {
                    double temp = values[j];
                    values[j] = values[j+1];
                    values[j+1] = temp;
                }
            }
        }

        PrintNumbers(values);


        Console.WriteLine($"Последовательность из {Count} чисел, экспоненциальный закон (lambda = {Lambda}):");
        PrintNumbers(values);

        double[] differences = ComputeDifferences(values);

        Console.WriteLine($"\nРазности соседних элементов Xi - X(i-1), количество = {differences.Length}:");
        PrintNumbers(differences);

        Console.WriteLine($"\nСреднее значение: {Mean(differences):F4}");
        Console.WriteLine($"Дисперсия:        {Variance(differences):F4}");

        // окно закроется - программа тоже закончится
        AppBuilder.Configure(() => new HistogramApp(differences, IntervalCount))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }

    // пустой ввод - случайная последовательность, мусор в консоли не роняет программу
    private static int? ReadSeed()
    {
        while (true)
        {
            Console.Write("Введите seed генератора (целое число, Enter - случайный): ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Seed не задан, последовательность будет случайной.");
                return null;
            }

            if (int.TryParse(input, out int seed))
                return seed;

            Console.WriteLine("Ошибка: нужно целое число. Повторите ввод.");
        }
    }

    // z = -ln(u) / lambda. u = 1 - NextDouble() лежит в (0; 1], ln(0) не возникает
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

    private static double Mean(double[] values)
    {
        double sum = 0;

        foreach (double x in values)
            sum += x;

        return sum / values.Length;
    }

    private static double Variance(double[] values)
    {
        double mean = Mean(values);
        double sumOfSquares = 0;

        foreach (double x in values)
            sumOfSquares += x * x;

        return sumOfSquares / values.Length - mean * mean;
    }

    private static void PrintNumbers(double[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Console.Write($"{values[i],9:F3}");

            if ((i + 1) % 5 == 0 || i == values.Length - 1)
                Console.WriteLine();
        }
    }
}
