// Lab1: Factory — фабрика создаёт объекты, клиент работает только с интерфейсом

// клиент не пишет new сам, а просит фабрику
IChatModel model = ChatModelFactory.Create("cloud");

string[] questions = { "Как хранить пароли?", "Что такое TCP?" };

foreach (string q in questions)
{
    Console.WriteLine("Вопрос: " + q);
    Console.WriteLine("Ответ:  " + model.Answer(q));
    Console.WriteLine();
}

// тот же код клиента, другая модель
IChatModel local = ChatModelFactory.Create("local");
Console.WriteLine("Ответ локальной модели: " + local.Answer(questions[0]));

// третий вариант фабрики — заглушка для тестов
IChatModel mock = ChatModelFactory.Create("mock");
Console.WriteLine("Ответ заглушки: " + mock.Answer(questions[0]));

public interface IChatModel
{
    string Answer(string question);
}

public class CloudModel : IChatModel
{
    public string Answer(string question)
    {
        Thread.Sleep(500); // имитация запроса в облако
        return "Облако отвечает развёрнуто на вопрос: " + question;
    }
}

public class LocalModel : IChatModel
{
    public string Answer(string question)
    {
        Thread.Sleep(200); // имитация инференса на своей видеокарте
        return "Локальная модель отвечает коротко: " + question;
    }
}

public class MockModel : IChatModel
{
    public string Answer(string question)
    {
        return "[заглушка] " + question;
    }
}

public static class ChatModelFactory
{
    public static IChatModel Create(string type)
    {
        if (type == "cloud") return new CloudModel();
        if (type == "local") return new LocalModel();
        if (type == "mock") return new MockModel();
        throw new Exception("Неизвестный тип модели: " + type);
    }
}
