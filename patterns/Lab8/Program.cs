using System;
using System.Collections.Generic;
using System.Linq;

namespace Lab8;

// ============================================================================
//  Лабораторная работа 8 — Strategy (поведенческий, через композицию)
//
//  Сюжет: текстовый генератор на каждой позиции выбирает следующий токен
//  из распределения вероятностей. КАК выбирать — стратегия:
//      greedy      — всегда самый вероятный токен (детерминированно, скучно)
//      temperature — «крутишь» распределение параметром T и сэмплируешь
//      top-k       — оставляешь k самых вероятных и сэмплируешь среди них
//      top-p       — nucleus: минимальный набор токенов с суммарной массой p
//  Генератор (контекст) не знает конкретных алгоритмов: стратегия подменяется
//  на лету, не переписывается ни строчка самого генератора.
// ============================================================================

public sealed record Candidate(string Token, double Probability);

// ----- Стратегия -----
public interface ISamplingStrategy
{
    string Name { get; }

    Candidate Select(IReadOnlyList<Candidate> distribution, Random rng);
}

// -- общая математика для всех стратегий --
public static class SamplingMath
{
    public static List<Candidate> Sorted(IReadOnlyList<Candidate> distribution)
        => distribution.OrderByDescending(c => c.Probability).ToList();

    public static List<Candidate> Renormalized(IEnumerable<Candidate> pool)
    {
        var list = pool.ToList();
        var sum = list.Sum(c => c.Probability);
        return list.Select(c => c with { Probability = c.Probability / sum }).ToList();
    }

    public static Candidate Roulette(IReadOnlyList<Candidate> pool, Random rng)
    {
        var roll = rng.NextDouble() * pool.Sum(c => c.Probability);
        var acc = 0.0;
        for (int i = 0; i < pool.Count - 1; i++)
        {
            acc += pool[i].Probability;
            if (roll < acc)
            {
                return pool[i];
            }
        }

        return pool[^1];
    }
}

public sealed class GreedyStrategy : ISamplingStrategy
{
    public string Name => "greedy (жадный выбор)";

    public Candidate Select(IReadOnlyList<Candidate> distribution, Random rng)
        => SamplingMath.Sorted(distribution)[0];
}

public sealed class TemperatureStrategy : ISamplingStrategy
{
    private readonly double _temperature;

    public TemperatureStrategy(double temperature) => _temperature = temperature;

    public string Name => $"temperature (T={_temperature:0.0#})";

    public Candidate Select(IReadOnlyList<Candidate> distribution, Random rng)
    {
        // p^(1/T): T<1 заостряет распределение, T>1 сглаживает
        var sharpened = distribution
            .Select(c => new Candidate(c.Token, Math.Pow(c.Probability, 1.0 / _temperature)));
        return SamplingMath.Roulette(SamplingMath.Renormalized(sharpened), rng);
    }
}

public sealed class TopKStrategy : ISamplingStrategy
{
    private readonly int _k;

    public TopKStrategy(int k) => _k = k;

    public string Name => $"top-k (k={_k})";

    public Candidate Select(IReadOnlyList<Candidate> distribution, Random rng)
    {
        var pool = SamplingMath.Renormalized(SamplingMath.Sorted(distribution).Take(_k));
        return SamplingMath.Roulette(pool, rng);
    }
}

public sealed class TopPStrategy : ISamplingStrategy
{
    private readonly double _p;

    public TopPStrategy(double p) => _p = p;

    public string Name => $"top-p / nucleus (p={_p:0.0#})";

    public Candidate Select(IReadOnlyList<Candidate> distribution, Random rng)
    {
        var sorted = SamplingMath.Sorted(distribution);
        var nucleus = new List<Candidate>();
        var mass = 0.0;

        foreach (var candidate in sorted)
        {
            nucleus.Add(candidate);
            mass += candidate.Probability;
            if (mass >= _p)
            {
                break;
            }
        }

        return SamplingMath.Roulette(SamplingMath.Renormalized(nucleus), rng);
    }
}

// ----- «Модель»: марковская цепочка с фиксированными распределениями -----
public sealed class FakeLanguageModel
{
    private static readonly string[] Starters = { "Наш", "Этот", "Лучший", "Новый" };
    private static readonly string[] Nouns = { "чат-бот", "ассистент", "нейросеть", "модель", "алгоритм" };
    private static readonly string[] Verbs = { "работает", "отвечает", "думает", "генерирует" };
    private static readonly string[] Adverbs = { "быстро", "круто", "стабильно" };

    public IReadOnlyList<Candidate> NextDistribution(string lastToken)
    {
        if (lastToken == "<start>")
        {
            return new List<Candidate>
            {
                new("Наш", 0.30), new("Этот", 0.25), new("Лучший", 0.25), new("Новый", 0.20),
            };
        }

        if (Starters.Contains(lastToken))
        {
            return Pool(Nouns, 0.40, 0.25, 0.20, 0.10, 0.05);
        }

        if (Nouns.Contains(lastToken))
        {
            return Pool(Verbs, 0.35, 0.30, 0.20, 0.15);
        }

        if (Verbs.Contains(lastToken))
        {
            return Pool(Adverbs, 0.40, 0.35, 0.25);
        }

        // после наречия — завершаем «предложение»
        return new List<Candidate>
        {
            new(".", 0.70), new("!", 0.20), new("…", 0.10),
        };
    }

    private static IReadOnlyList<Candidate> Pool(string[] tokens, params double[] probabilities)
        => tokens.Zip(probabilities, (t, p) => new Candidate(t, p)).ToList();
}

// ----- Контекст: генератор держит стратегию и умеет её менять -----
public sealed class TextGenerator
{
    private readonly FakeLanguageModel _model = new();
    private readonly Random _rng = new(42); // фиксированный seed — воспроизводимость

    public ISamplingStrategy Sampling { get; set; } = new GreedyStrategy();

    public string Generate(int maxTokens = 10)
    {
        var last = "<start>";
        var words = new List<string>();

        for (int i = 0; i < maxTokens; i++)
        {
            var chosen = Sampling.Select(_model.NextDistribution(last), _rng);
            words.Add(chosen.Token);
            last = chosen.Token;

            if (chosen.Token is "." or "!" or "…")
            {
                break;
            }
        }

        return string.Join(" ", words)
            .Replace(" .", ".")
            .Replace(" !", "!")
            .Replace(" …", "…");
    }
}

public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("Распределение после токена «Наш»:");
        var demoModel = new FakeLanguageModel();
        foreach (var c in demoModel.NextDistribution("Наш"))
        {
            Console.WriteLine($"    {c.Token,-12} p = {c.Probability:0.00}");
        }

        var generator = new TextGenerator();

        Console.WriteLine("\nОдин и тот же seed, разные стратегии сэмплирования:\n");
        ISamplingStrategy[] strategies =
        {
            new GreedyStrategy(),
            new TemperatureStrategy(0.5),
            new TemperatureStrategy(1.5),
            new TopKStrategy(2),
            new TopKStrategy(4),
            new TopPStrategy(0.5),
            new TopPStrategy(0.9),
        };

        foreach (var strategy in strategies)
        {
            generator.Sampling = strategy; // подмена стратегии на лету
            Console.WriteLine($"{strategy.Name,-32} -> {generator.Generate()}");
        }

        Console.WriteLine("\nВывод: контекст (TextGenerator) не менялся вообще — менялись только стратегии.");
    }
}
