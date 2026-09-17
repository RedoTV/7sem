using System;
using System.Collections.Generic;
using System.Linq;

namespace Lab12;

// ============================================================================
//  Лабораторная работа 12 — Specification (поведенческий, контракты)
//
//  Сюжет: каталог LLM-моделей маркетплейса. Задачи вида «найди модели
//  с контекстом от 8K, дешевле $0.50 за 1K и откликом до 500 мс» не должны
//  превращаться в простыню if-ов. Критерий выбора — объект-спецификация,
//  которую можно свободно комбинировать через And / Or / Not.
// ============================================================================

public sealed record AiModel(
    string Name,
    string Vendor,
    int ContextWindow,
    decimal PricePer1k,
    int AvgLatencyMs,
    bool Vision,
    bool Tools,
    bool OpenWeights)
{
    public override string ToString()
        => $"{Name,-12} | {Vendor,-9} | ctx {ContextWindow,7:N0} | ${PricePer1k,5:0.00}/1K | {AvgLatencyMs,4} мс | " +
           $"{(Vision ? "vision" : "------")} {(Tools ? "tools" : "-----")} {(OpenWeights ? "open" : "----")}";
}

// ----- Контракт спецификации -----
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T item);
}

// ----- Комбинаторы -----
public sealed class AndSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public bool IsSatisfiedBy(T item) => _left.IsSatisfiedBy(item) && _right.IsSatisfiedBy(item);
}

public sealed class OrSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public bool IsSatisfiedBy(T item) => _left.IsSatisfiedBy(item) || _right.IsSatisfiedBy(item);
}

public sealed class NotSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _inner;

    public NotSpecification(ISpecification<T> inner) => _inner = inner;

    public bool IsSatisfiedBy(T item) => !_inner.IsSatisfiedBy(item);
}

public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
        => new AndSpecification<T>(left, right);

    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
        => new OrSpecification<T>(left, right);

    public static ISpecification<T> Not<T>(this ISpecification<T> spec)
        => new NotSpecification<T>(spec);
}

// ----- Конкретные спецификации домена -----
public sealed class MinContextSpec : ISpecification<AiModel>
{
    private readonly int _minTokens;

    public MinContextSpec(int minTokens) => _minTokens = minTokens;

    public bool IsSatisfiedBy(AiModel item) => item.ContextWindow >= _minTokens;
}

public sealed class MaxPriceSpec : ISpecification<AiModel>
{
    private readonly decimal _maxPer1k;

    public MaxPriceSpec(decimal maxPer1k) => _maxPer1k = maxPer1k;

    public bool IsSatisfiedBy(AiModel item) => item.PricePer1k <= _maxPer1k;
}

public sealed class MaxLatencySpec : ISpecification<AiModel>
{
    private readonly int _maxMs;

    public MaxLatencySpec(int maxMs) => _maxMs = maxMs;

    public bool IsSatisfiedBy(AiModel item) => item.AvgLatencyMs <= _maxMs;
}

public sealed class VisionSpec : ISpecification<AiModel>
{
    public bool IsSatisfiedBy(AiModel item) => item.Vision;
}

public sealed class ToolsSpec : ISpecification<AiModel>
{
    public bool IsSatisfiedBy(AiModel item) => item.Tools;
}

public sealed class OpenWeightsSpec : ISpecification<AiModel>
{
    public bool IsSatisfiedBy(AiModel item) => item.OpenWeights;
}

public sealed class VendorSpec : ISpecification<AiModel>
{
    private readonly string _vendor;

    public VendorSpec(string vendor) => _vendor = vendor;

    public bool IsSatisfiedBy(AiModel item) => item.Vendor == _vendor;
}

public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var catalog = new List<AiModel>
        {
            new("Titan-Ultra", "CloudCorp", 200_000, 12.00m, 950,  Vision: true,  Tools: true,  OpenWeights: false),
            new("Titan-Mini",  "CloudCorp",  32_000,  0.35m, 420,  Vision: false, Tools: true,  OpenWeights: false),
            new("Omni-8B",     "OpenMind",   32_000,  0.20m, 610,  Vision: true,  Tools: true,  OpenWeights: true),
            new("MiniMind-3B", "OpenMind",    8_000,  0.05m, 240,  Vision: false, Tools: false, OpenWeights: true),
            new("VisionPro-2", "NeuroTech",  64_000,  2.50m, 700,  Vision: true,  Tools: true,  OpenWeights: false),
            new("NanoSwift",   "NeuroTech",   4_000,  0.02m,  90,  Vision: false, Tools: false, OpenWeights: false),
            new("Legacy-1",    "OldSoft",     2_048,  0.10m, 1500, Vision: false, Tools: false, OpenWeights: false),
            new("CodeMate-X",  "NeuroTech", 128_000,  1.20m, 500,  Vision: false, Tools: true,  OpenWeights: false),
        };

        Console.WriteLine("Каталог моделей маркетплейса:");
        foreach (var model in catalog)
        {
            Console.WriteLine("  " + model);
        }

        Show("1) Бюджетный прод-чат-бот: ctx >= 8K  И  цена <= $0.50  И  отклик <= 500 мс",
            new MinContextSpec(8_000).And(new MaxPriceSpec(0.50m)).And(new MaxLatencySpec(500)),
            catalog);

        Show("2) Мультимодальный ассистент: vision  И  вызов инструментов",
            new VisionSpec().And(new ToolsSpec()),
            catalog);

        Show("3) Приватный контур: открытые веса  И  НЕ CloudCorp",
            new OpenWeightsSpec().And(new VendorSpec("CloudCorp").Not()),
            catalog);

        Show("4) «Тяжёлая артиллерия»: НЕ (дешёвые ИЛИ быстрые) = дорогая И медленная",
            new MaxPriceSpec(1.00m).Or(new MaxLatencySpec(800)).Not(),
            catalog);

        Show("5) Длинный контекст за разумные деньги: ctx >= 50K  И  цена <= $2.00",
            new MinContextSpec(50_000).And(new MaxPriceSpec(2.00m)),
            catalog);

        // Спецификации отлично дружат с LINQ: автоподбор лучшей модели под задачу
        var best = catalog
            .Where(new MinContextSpec(50_000).And(new MaxLatencySpec(600)).IsSatisfiedBy)
            .OrderBy(m => m.PricePer1k)
            .FirstOrDefault();

        Console.WriteLine("\n6) Автоподбор: самая дешёвая из моделей с ctx >= 50K и откликом <= 600 мс");
        Console.WriteLine(best is null
            ? "   (подходящих нет)"
            : $"   выбрана: {best.Name} (${best.PricePer1k:0.00}/1K, {best.AvgLatencyMs} мс)");
    }

    private static void Show(string title, ISpecification<AiModel> spec, IReadOnlyList<AiModel> catalog)
    {
        Console.WriteLine($"\n— {title}");
        var matches = catalog.Where(spec.IsSatisfiedBy).ToList();
        if (matches.Count == 0)
        {
            Console.WriteLine("  (подходящих моделей нет)");
            return;
        }

        foreach (var model in matches)
        {
            Console.WriteLine("  " + model);
        }
    }
}
