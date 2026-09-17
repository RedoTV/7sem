using System;
using System.Collections.Generic;
using System.Linq;

namespace Lab4;

public interface IReasoner
{
    string Think(string userMessage, IReadOnlyList<string> history);
}

public interface IMemory
{
    void Remember(string role, string text);
    IReadOnlyList<string> History { get; }
}

public interface ISkill
{
    bool CanHandle(string message);
    string Execute(string message);
}

public sealed class EchoReasoner : IReasoner 
{
    public string Think(string userMessage, IReadOnlyList<string> history)
        => $"(echo-модель) Вы спросили: \"{userMessage}\". Отвечаю эхом.";
}

public sealed class PremiumReasoner : IReasoner 
{
    public string Think(string userMessage, IReadOnlyList<string> history)
        => $"(premium-LLM) Рассуждаю над \"{userMessage}\" с учётом {history.Count} реплик контекста...";
}

public class VolatileMemory : IMemory
{
    private readonly List<string> _history = new();

    public void Remember(string role, string text) => _history.Add($"[{role}] {text}");

    public IReadOnlyList<string> History => _history.ToList();
}

public sealed class ClockSkill : ISkill
{
    public bool CanHandle(string message)
        => message.Contains("который час", StringComparison.OrdinalIgnoreCase);

    public string Execute(string message) => $"сейчас {DateTime.Now:HH:mm:ss}";
}

public sealed class AiAssistant
{
    private readonly IMemory _memory;
    private readonly ISkill? _skill;

    public IReasoner Reasoner { get; set; } 

    public AiAssistant(IReasoner reasoner, IMemory memory, ISkill? skill = null)
    {
        Reasoner = reasoner;
        _memory = memory;
        _skill = skill;
    }

    public string Handle(string userMessage)
    {
        _memory.Remember("user", userMessage);

        var answer = _skill is not null && _skill.CanHandle(userMessage)
            ? _skill.Execute(userMessage)
            : Reasoner.Think(userMessage, _memory.History);

        _memory.Remember("assistant", answer);
        return answer;
    }
}

public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== Ассистент собран из модулей: echo-мозг + память + навык-часы ===");
        var assistant = new AiAssistant(
            reasoner: new EchoReasoner(),
            memory: new VolatileMemory(),
            skill: new ClockSkill());

        Console.WriteLine(assistant.Handle("который час?"));
        Console.WriteLine(assistant.Handle("объясни идею композиции"));

        Console.WriteLine("\n=== Горячая замена модуля: другой мозг, та же сборка ===");
        assistant.Reasoner = new PremiumReasoner();
        Console.WriteLine(assistant.Handle("и что изменилось?"));

        Console.WriteLine("\n=== Ассистент B: те же запчасти, другая комбинация (без единого нового подкласса) ===");
        var pro = new AiAssistant(reasoner: new PremiumReasoner(), memory: new VolatileMemory());
        Console.WriteLine(pro.Handle("а наследование чем хуже?"));
    }
}
