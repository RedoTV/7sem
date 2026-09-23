// Lab11: Visitor — операции отделены от иерархии элементов.
// Основа обобщённая: IVisitable<TVisitor> + IChatVisitor, три операции-визитора
// (расшифровка, стоимость, JSON) не трогают классы элементов.

using System.Text;

List<IChatElement> chat = new List<IChatElement>
{
    new UserMessage("Найди все вызовы PostAsync"),
    new AssistantMessage("Ищу...", 100, 20),
    new ToolCall("search_code", "PostAsync", 450),
    new AssistantMessage("Нашёл 7 вызовов", 600, 90),
    new ImageAttachment("architecture.png", 1920, 1080, 1440),
    new UserMessage("Объясни диаграмму")
};

// одна и та же иерархия — три разные операции
List<ChatVisitorBase> visitors = new List<ChatVisitorBase>
{
    new TranscriptVisitor(),
    new CostVisitor(),
    new JsonVisitor() // добавлен позже, классы элементов не менялись
};

foreach (ChatVisitorBase visitor in visitors)
{
    foreach (IChatElement element in chat)
    {
        element.Accept(visitor); // двойная диспетчеризация
    }
    Console.WriteLine(visitor.BuildReport());
    Console.WriteLine();
}

// ================== обобщённая основа паттерна ==================

// visitable-объект знает, какому типу визитора себя передать
public interface IVisitable<TVisitor>
{
    void Accept(TVisitor visitor);
}

// домен: элементы диалога + свой интерфейс визитора
public interface IChatElement : IVisitable<IChatVisitor> { }

public interface IChatVisitor
{
    void Visit(UserMessage m);
    void Visit(AssistantMessage m);
    void Visit(ToolCall m);
    void Visit(ImageAttachment m);
}

// база для операций: посещает все типы + умеет выдать отчёт
public abstract class ChatVisitorBase : IChatVisitor
{
    public abstract void Visit(UserMessage m);
    public abstract void Visit(AssistantMessage m);
    public abstract void Visit(ToolCall m);
    public abstract void Visit(ImageAttachment m);

    public abstract string BuildReport();
}

// ================== иерархия элементов (закрыта для изменения) ==================

public sealed class UserMessage : IChatElement
{
    public string Text;

    public UserMessage(string text) { Text = text; }

    public void Accept(IChatVisitor visitor) { visitor.Visit(this); }
}

public sealed class AssistantMessage : IChatElement
{
    public string Text;
    public int InputTokens;
    public int OutputTokens;

    public AssistantMessage(string text, int inputTokens, int outputTokens)
    {
        Text = text;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    public void Accept(IChatVisitor visitor) { visitor.Visit(this); }
}

public sealed class ToolCall : IChatElement
{
    public string Name;
    public string Arguments;
    public int Tokens;

    public ToolCall(string name, string arguments, int tokens)
    {
        Name = name;
        Arguments = arguments;
        Tokens = tokens;
    }

    public void Accept(IChatVisitor visitor) { visitor.Visit(this); }
}

public sealed class ImageAttachment : IChatElement
{
    public string FileName;
    public int Width;
    public int Height;
    public int Tokens;

    public ImageAttachment(string fileName, int width, int height, int tokens)
    {
        FileName = fileName;
        Width = width;
        Height = height;
        Tokens = tokens;
    }

    public long Megapixels
    {
        get { return (long)Width * Height / 1_000_000; }
    }

    public void Accept(IChatVisitor visitor) { visitor.Visit(this); }
}

// ================== операции-визиторы ==================

// операция 1: текстовая расшифровка диалога
public sealed class TranscriptVisitor : ChatVisitorBase
{
    private readonly StringBuilder _sb = new StringBuilder();

    public override void Visit(UserMessage m) { _sb.AppendLine("[Пользователь] " + m.Text); }
    public override void Visit(AssistantMessage m) { _sb.AppendLine("[Ассистент] " + m.Text); }
    public override void Visit(ToolCall m) { _sb.AppendLine("[Инструмент " + m.Name + "] вызов: " + m.Arguments); }
    public override void Visit(ImageAttachment m) { _sb.AppendLine("[Картинка] " + m.FileName + " " + m.Width + "x" + m.Height); }

    public override string BuildReport() { return _sb.ToString(); }
}

// операция 2: подсчёт стоимости по тарифам
public sealed class CostVisitor : ChatVisitorBase
{
    private const decimal InputPer1K = 0.0025m;   // входные токены, $ за 1000
    private const decimal OutputPer1K = 0.01m;    // выходные дороже
    private const decimal PerMegapixel = 0.003m;  // картинки — за мегапиксель

    private int _input;
    private int _output;
    private decimal _cost;

    public override void Visit(UserMessage m)
    {
        int tokens = m.Text.Length / 2;
        _input += tokens;
        _cost += tokens * InputPer1K / 1000m;
    }

    public override void Visit(AssistantMessage m)
    {
        _input += m.InputTokens;
        _output += m.OutputTokens;
        _cost += (m.InputTokens * InputPer1K + m.OutputTokens * OutputPer1K) / 1000m;
    }

    public override void Visit(ToolCall m)
    {
        _input += m.Tokens;
        _cost += m.Tokens * InputPer1K / 1000m;
    }

    public override void Visit(ImageAttachment m)
    {
        _input += m.Tokens;
        _cost += m.Megapixels * PerMegapixel;
    }

    public override string BuildReport()
    {
        return "Вход: " + _input + " ток., выход: " + _output +
               " ток., стоимость: $" + Math.Round(_cost, 6);
    }
}

// операция 3: экспорт в JSON — новый алгоритм, иерархия не менялась
public sealed class JsonVisitor : ChatVisitorBase
{
    private readonly List<string> _items = new List<string>();

    public override void Visit(UserMessage m) { Add("user", m.Text, m.Text.Length / 2); }
    public override void Visit(AssistantMessage m) { Add("assistant", m.Text, m.InputTokens + m.OutputTokens); }
    public override void Visit(ToolCall m) { Add("tool", m.Name + "(" + m.Arguments + ")", m.Tokens); }
    public override void Visit(ImageAttachment m) { Add("image", m.FileName, m.Tokens); }

    private void Add(string role, string content, int tokens)
    {
        _items.Add("  { \"role\": \"" + role + "\", \"tokens\": " + tokens + ", \"content\": \"" + content + "\" }");
    }

    public override string BuildReport()
    {
        return "[\n" + string.Join(",\n", _items) + "\n]";
    }
}
