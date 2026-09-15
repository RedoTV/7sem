using Lab1;

ChatModelFactory.Register("offline", () => new MockEchoModel());

string provider = args.Length > 0 ? args[0] : "cloud";
Console.WriteLine($"Провайдер из конфигурации: {provider}\n");

var session = new ChatSession(ChatModelFactory.Create(provider));
session.RunDemo();

Console.WriteLine("\n--------- тот же код клиента, но с локальной моделью ---------");
new ChatSession(ChatModelFactory.Create("local")).RunDemo();

public class ChatSession(IChatModel model)
{
    private readonly IChatModel _model = model;

    private static readonly string[] Questions =
    [
        "Как хранить пароли пользователей?",
        "Почему git merge конфликтует?",
        "Объясни разницу между TCP и UDP"
    ];

    public void RunDemo()
    {
        decimal spent = 0m;

        foreach (string question in Questions)
        {
            ChatReply reply = _model.Complete(new ChatRequest(question));
            spent += reply.CostUsd;

            Console.WriteLine($"[Вопрос ] {question}");
            Console.WriteLine($"[Модель ] {_model.Name}");
            Console.WriteLine($"[Ответ  ] {reply.Text}");
            Console.WriteLine($"[Токены ] {reply.TokensUsed}, стоимость: ${reply.CostUsd:0.000000}\n");
        }

        Console.WriteLine($"Итого за сессию: ${spent:0.000000}");
    }
}
