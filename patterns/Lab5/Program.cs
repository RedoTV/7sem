// Lab5: Proxy — прокси стоит между клиентом и сервисом и кеширует ответы

ILlmService service = new CachingProxy();

Console.WriteLine(service.Complete("Что такое градиентный спуск?")); // медленно, идём в сеть
Console.WriteLine(service.Complete("Что такое градиентный спуск?")); // быстро, из кеша
Console.WriteLine(service.Complete("Что такое backpropagation?"));   // снова в сеть

public interface ILlmService
{
    string Complete(string prompt);
}

// настоящий удалённый сервис
public class RemoteLlmService : ILlmService
{
    public string Complete(string prompt)
    {
        Console.WriteLine("(идём в облако, ждём ответ...)");
        Thread.Sleep(1000); // имитация задержки сети
        return "Ответ модели: " + prompt;
    }
}

// прокси: тот же интерфейс, но сначала смотрит в кеш
public class CachingProxy : ILlmService
{
    private RemoteLlmService real = new RemoteLlmService();
    private Dictionary<string, string> cache = new Dictionary<string, string>();

    public string Complete(string prompt)
    {
        if (cache.ContainsKey(prompt))
        {
            Console.WriteLine("(ответ из кеша)");
            return cache[prompt];
        }

        string answer = real.Complete(prompt);
        cache[prompt] = answer;
        return answer;
    }
}
