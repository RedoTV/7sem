using System.Diagnostics;
using Lab5;

// клиенту подсовываем прокси вместо сервиса — для него разницы нет
ILlmService service = new CachingLlmProxy(new RemoteLlmService());

var sw = Stopwatch.StartNew();

Console.WriteLine("1) Первое обращение — в кеше пусто:");
Ask(service, "Как работает градиентный спуск?");
PrintTime(sw);

Console.WriteLine("\n2) Повторяем дословно тот же запрос:");
Ask(service, "Как работает градиентный спуск?");
PrintTime(sw);

Console.WriteLine("\n3) Другой вопрос — новый ключ кеша:");
Ask(service, "Что такое backpropagation?");
PrintTime(sw);

Console.WriteLine("\n4) Тот же вопрос, но temperature=0.1 — ключ другой:");
Ask(service, "Как работает градиентный спуск?", temperature: 0.1);
PrintTime(sw);

Console.WriteLine("\n5) Короткий TTL: ждём протухания и спрашиваем снова…");
var shortTtl = new CachingLlmProxy(new RemoteLlmService(), ttl: TimeSpan.FromSeconds(1));
Ask(shortTtl, "Какие бывают активации?");
await Task.Delay(1500);
Ask(shortTtl, "Какие бывают активации?");
shortTtl.PrintStats();

Console.WriteLine("\n===== Итог =====");
((CachingLlmProxy)service).PrintStats();
Console.WriteLine($"Реальных походов в сеть всего: {RemoteLlmService.NetworkCalls}. " +
                  "Из них основной прокси (4 запроса) потратил только 3 — второй запрос из кеша.");

static void Ask(ILlmService service, string prompt, double temperature = 0.7)
{
    LlmResponse response = service.Complete(new LlmRequest("gpt-4o", prompt, temperature));
    Console.WriteLine($"   Ответ: {response.Text} ({response.TokensUsed} ток.)");
}

static void PrintTime(Stopwatch sw) =>
    Console.WriteLine($"   Прошло с начала демо: {sw.ElapsedMilliseconds} мс\n");
