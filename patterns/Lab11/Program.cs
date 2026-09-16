// Lab11: Visitor — добавляем новые операции к набору классов, не меняя сами классы

List<IChatElement> chat = new List<IChatElement>();
chat.Add(new UserMessage("Привет, найди все вызовы PostAsync"));
chat.Add(new AssistantMessage("Ищу...", 100, 20));
chat.Add(new AssistantMessage("Нашёл 7 вызовов", 500, 80));

Console.WriteLine("--- Расшифровка диалога ---");
VisitorPrint printer = new VisitorPrint();
foreach (IChatElement element in chat)
{
    element.Accept(printer);
}

Console.WriteLine();
Console.WriteLine("--- Подсчёт стоимости ---");
VisitorCost counter = new VisitorCost();
foreach (IChatElement element in chat)
{
    element.Accept(counter);
}
counter.PrintTotal();

public interface IChatElement
{
    void Accept(IChatElementVisitor visitor);
}

public class UserMessage : IChatElement
{
    public string Text;

    public UserMessage(string text)
    {
        Text = text;
    }

    public void Accept(IChatElementVisitor visitor)
    {
        visitor.VisitUser(this);
    }
}

public class AssistantMessage : IChatElement
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

    public void Accept(IChatElementVisitor visitor)
    {
        visitor.VisitAssistant(this);
    }
}

public interface IChatElementVisitor
{
    void VisitUser(UserMessage m);
    void VisitAssistant(AssistantMessage m);
}

// операция 1: печать диалога
public class VisitorPrint : IChatElementVisitor
{
    public void VisitUser(UserMessage m)
    {
        Console.WriteLine("[Пользователь] " + m.Text);
    }

    public void VisitAssistant(AssistantMessage m)
    {
        Console.WriteLine("[Ассистент] " + m.Text);
    }
}

// операция 2: подсчёт стоимости
public class VisitorCost : IChatElementVisitor
{
    private int totalTokens = 0;

    public void VisitUser(UserMessage m)
    {
        totalTokens = totalTokens + m.Text.Length / 2;
    }

    public void VisitAssistant(AssistantMessage m)
    {
        totalTokens = totalTokens + m.InputTokens + m.OutputTokens;
    }

    public void PrintTotal()
    {
        Console.WriteLine("Всего токенов: " + totalTokens);
        Console.WriteLine("Стоимость: $" + totalTokens * 0.00001);
    }
}
