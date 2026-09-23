// Лабораторная работа №1, вариант 7.
//
// Ищем наименьшее натуральное решение a^2 + b^2 = c^2 + d^2, a != b, c != d.
//
// Сумму s перебираем с 5 вверх (5 = 1^2 + 2^2 - самая маленькая сумма двух
// разных квадратов). Для каждой s собираем пары (x, y), y > x, у которых
// x^2 + y^2 = s. y считаем через Sqrt и сразу проверяем равенство обратно:
// корень округляется, и без проверки пара может оказаться лишней.
// Первая сумма, у которой таких пар хотя бы две, и есть наименьшее решение:
// меньшие суммы уже проверены.

namespace Lab1;

public static class Program
{
    private const int MinSum = 5;      // меньше двух разных квадратов не бывает
    private const int MaxSum = 10000;  // защищенность: дальше перебирать в этой работе незачем

    public static void Main()
    {
        Console.WriteLine("Поиск наименьшего натурального решения a^2 + b^2 = c^2 + d^2 (a != b, c != d)."); // информативность

        int? limit = ReadLimit();
        if (limit == null)
            return;

        var solution = FindSmallestSolution(limit.Value);

        if (solution == null)
            Console.WriteLine($"Решение при сумме от {MinSum} до {limit} не найдено.");
        else
            PrintSolution(solution.Value);
    }

    // устойчивость: пустой ввод и не число не роняют программу
    // коммуникабельность: в подсказке сразу указан допустимый диапазон
    private static int? ReadLimit()
    {
        while (true)
        {
            Console.Write($"Введите верхнюю границу суммы s (от {MinSum} до {MaxSum}): ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Ввод прерван.");
                return null;
            }

            if (!int.TryParse(input, out int limit))
            {
                Console.WriteLine("Ошибка: нужно целое число. Повторите ввод.");
                continue;
            }

            if (limit < MinSum || limit > MaxSum)
            {
                Console.WriteLine($"Ошибка: число должно быть от {MinSum} до {MaxSum}.");
                continue;
            }

            return limit;
        }
    }

    // временная эффективность: суммы идут снизу вверх, после первой подходящей поиск заканчивается
    private static (int a, int b, int c, int d, int s)? FindSmallestSolution(int limit)
    {
        for (int s = MinSum; s <= limit; s++)
        {
            var pairs = PairsWithSquareSum(s);

            if (pairs.Count >= 2)
                return (pairs[0].x, pairs[0].y, pairs[1].x, pairs[1].y, s);
        }

        return null;
    }

    // y > x отсекает равные числа и не дает посчитать одну пару дважды
    private static List<(int x, int y)> PairsWithSquareSum(int s)
    {
        var pairs = new List<(int x, int y)>();

        for (int x = 1; 2 * x * x < s; x++)
        {
            int y = (int)Math.Sqrt(s - x * x);

            if (y > x && x * x + y * y == s) // точность: Sqrt округляет, равенство проверяем заново
                pairs.Add((x, y));
        }

        return pairs;
    }

    private static void PrintSolution((int a, int b, int c, int d, int s) solution)
    {
        var (a, b, c, d, s) = solution;

        // надежность: перед печатью еще раз сверяем найденные числа
        if (a == b || c == d || a * a + b * b != s || c * c + d * d != s)
        {
            Console.WriteLine("Внутренняя ошибка: решение не сошлось.");
            return;
        }

        Console.WriteLine($"Наименьшее решение: {a}^2 + {b}^2 = {c}^2 + {d}^2 = {s}");
    }
}
