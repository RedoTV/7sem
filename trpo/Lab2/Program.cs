// ============================================================================
// Лабораторная работа №2. Вариант 7. "Стиль программирования"
// Задание: сгенерировать последовательность 100 случайных чисел с
//          экспоненциальным законом распределения (lambda = 0,5).
//          Упорядочить по возрастанию. Образовать новую последовательность
//          из разности соседних элементов Xi - X(i-1). Для нее вычислить
//          среднее значение и дисперсию, вывести гистограмму,
//          разбив диапазон на 10 интервалов.
//
// КАК РАБОТАЕТ АЛГОРИТМ:
//  Шаг 1. Генерируем u - числа, равномерно распределенные в (0; 1].
//  Шаг 2. Преобразуем их в экспоненциальное распределение по формуле
//         из задания: z = -ln(u) / lambda.
//         (берем u = 1 - NextDouble(), чтобы u не оказался равен 0,
//         иначе ln(0) не существует - устойчивость).
//  Шаг 3. Сортируем последовательность по возрастанию.
//  Шаг 4. Строим разности соседних элементов: d[i] = X[i] - X[i-1],
//         получается 99 значений.
//  Шаг 5. Считаем характеристики по формулам из задания:
//         среднее  mx = (1/k) * SUM(xi);
//         дисперсия dx = (1/k) * SUM(xi^2) - mx^2.
//  Шаг 6. Рисуем текстовую гистограмму: делим диапазон [min; max] на 10
//         равных интервалов, считаем попадания, длину столбика
//         пропорционально масштабируем символами '#'.
//
// ПРАВИЛА ХОРОШЕГО СТИЛЯ, КОТОРЫЕ СОБЛЮДЕНЫ:
//   комментарии       - XML-описание у каждого метода и шапка файла,
//                       поясняющая алгоритм целиком;
//   осмысленные имена - GenerateExponentialSequence, ComputeDifferences,
//                       Mean, Variance, PrintHistogram: из имени метода
//                       или переменной понятно его назначение;
//   отступы           - единый отступ (4 пробела) на каждый уровень
//                       вложенности, точно повторяющий логическую
//                       структуру кода;
//   пустые строки     - отделяют логические блоки внутри методов
//                       и методы друг от друга;
//   единообразие      - одинаковый стиль скобок и форматирования во всем
//                       файле, исправление одной строки не ломает соседние;
//   модульность       - каждый шаг алгоритма оформлен отдельным методом
//                       и его можно проверить независимо от остальных.
// ============================================================================
namespace Lab2;

/// <summary>
/// Лабораторная работа №2, вариант 7.
/// Экспоненциальное распределение (lambda = 0,5), разности соседних
/// элементов, среднее, дисперсия и гистограмма из 10 интервалов.
/// </summary>
public static class Program
{
    /// <summary>Количество генерируемых случайных чисел.</summary>
    private const int Count = 100;

    /// <summary>Параметр экспоненциального распределения.</summary>
    private const double Lambda = 0.5;

    /// <summary>Число интервалов гистограммы.</summary>
    private const int IntervalCount = 10;

    /// <summary>Максимальная длина столбика гистограммы в символах.</summary>
    private const int MaxBarLength = 40;

    /// <summary>Точка входа: генерация, сортировка, разности, характеристики, гистограмма.</summary>
    public static void Main()
    {
        PrintDescription();

        int? seed = ReadSeed(); // null - seed выбирается случайно

        // Шаги 1-3: генерация и сортировка
        double[] generated = GenerateExponentialSequence(Count, Lambda, seed);
        Array.Sort(generated);
        PrintSequence($"Отсортированная последовательность (n = {Count}, lambda = {Lambda}):",
                      generated, perLine: 5);

        // Шаг 4: разности соседних элементов
        double[] differences = ComputeDifferences(generated);
        PrintSequence($"Разности соседних элементов (количество = {differences.Length}):",
                      differences, perLine: 5);

        // Шаг 5: характеристики
        Console.WriteLine();
        Console.WriteLine($"Среднее значение: {Mean(differences):F4}");
        Console.WriteLine($"Дисперсия:        {Variance(differences):F4}");

        // Шаг 6: гистограмма
        PrintHistogram(differences, IntervalCount);
    }

    /// <summary>Печатает краткое описание программы (информативность).</summary>
    private static void PrintDescription()
    {
        Console.WriteLine("Программа генерирует 100 случайных чисел с экспоненциальным распределением");
        Console.WriteLine("(lambda = 0,5), сортирует их, строит разности соседних элементов и выводит");
        Console.WriteLine("среднее значение, дисперсию и гистограмму из 10 интервалов.");
    }

    /// <summary>
    /// Запрашивает seed генератора. Пустой ввод - случайный seed
    /// (коммуникабельность, устойчивость).
    /// </summary>
    /// <returns>Введенный seed или null, если нужен случайный.</returns>
    private static int? ReadSeed()
    {
        while (true)
        {
            Console.Write("Введите seed генератора (целое число, Enter - случайный): ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) // Enter или закрытый ввод
            {
                Console.WriteLine("Используется случайный seed.\n");
                return null;
            }

            if (!int.TryParse(input, out int seed)) // не число - не падаем
            {
                Console.WriteLine("Ошибка: нужно целое число. Повторите ввод.");
                continue;
            }

            return seed;
        }
    }

    /// <summary>
    /// Генерирует count чисел с экспоненциальным распределением
    /// z = -ln(u) / lambda, где u равномерно в (0; 1].
    /// Одинаковый seed дает одинаковую последовательность (воспроизводимость).
    /// </summary>
    private static double[] GenerateExponentialSequence(int count, double lambda, int? seed)
    {
        Random random = seed.HasValue ? new Random(seed.Value) : Random.Shared;

        var values = new double[count];

        for (int i = 0; i < count; i++)
        {
            double u = 1.0 - random.NextDouble(); // u в (0; 1]: исключили u = 0
            values[i] = -Math.Log(u) / lambda;
        }

        return values;
    }

    /// <summary>
    /// Строит последовательность разностей соседних элементов
    /// отсортированного массива: d[i] = x[i] - x[i-1].
    /// </summary>
    private static double[] ComputeDifferences(double[] sorted)
    {
        var differences = new double[sorted.Length - 1];

        for (int i = 1; i < sorted.Length; i++)
            differences[i - 1] = sorted[i] - sorted[i - 1];

        return differences;
    }

    /// <summary>Среднее значение: mx = (1/k) * SUM(xi).</summary>
    private static double Mean(double[] values)
    {
        double sum = 0;

        foreach (double x in values)
            sum += x;

        return sum / values.Length;
    }

    /// <summary>Дисперсия: dx = (1/k) * SUM(xi^2) - mx^2.</summary>
    private static double Variance(double[] values)
    {
        double sumOfSquares = 0;

        foreach (double x in values)
            sumOfSquares += x * x;

        return sumOfSquares / values.Length - Math.Pow(Mean(values), 2);
    }

    /// <summary>
    /// Печатает гистограмму: делит диапазон [min; max] на intervalCount
    /// равных интервалов, считает попадания и рисует столбики '#'.
    /// Перед выводом проверяет, что учтены все значения (надежность).
    /// </summary>
    private static void PrintHistogram(double[] values, int intervalCount)
    {
        double min = values.Min();
        double max = values.Max();
        double width = (max - min) / intervalCount;

        var counts = new int[intervalCount];

        foreach (double x in values)
        {
            int index = (int)((x - min) / width);

            if (index == intervalCount) // x == max попадает в последний интервал
                index--;

            counts[index]++;
        }

        // самопроверка: все значения должны быть распределены по интервалам
        if (counts.Sum() != values.Length)
        {
            Console.WriteLine("Внутренняя ошибка: не все значения попали в интервалы.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Гистограмма ({intervalCount} интервалов):");

        int maxCount = counts.Max();

        for (int i = 0; i < intervalCount; i++)
        {
            double from = min + i * width;
            double to = from + width;

            // длина столбика пропорциональна количеству попаданий
            int barLength = maxCount == 0
                ? 0
                : (int)Math.Round((double)counts[i] / maxCount * MaxBarLength);

            Console.WriteLine($"[{from,8:F3}; {to,8:F3}) | {new string('#', barLength)} {counts[i]}");
        }
    }

    /// <summary>Печатает массив чисел по perLine значений в строке.</summary>
    private static void PrintSequence(string title, double[] values, int perLine)
    {
        Console.WriteLine();
        Console.WriteLine(title);

        for (int i = 0; i < values.Length; i++)
        {
            Console.Write($"{values[i],9:F3} ");

            if ((i + 1) % perLine == 0)
                Console.WriteLine();
        }

        if (values.Length % perLine != 0)
            Console.WriteLine();
    }
}
