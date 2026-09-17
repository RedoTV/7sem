using System;
using System.Linq;
using System.Threading;

namespace Lab2;

public sealed class TokenLedger
{
    private static volatile TokenLedger? _instance;
    private static readonly object Gate = new();

    public static int CreatedCount;

    private long _totalTokens;

    private TokenLedger() 
    {
        Interlocked.Increment(ref CreatedCount);
        Console.WriteLine("  [TokenLedger] экземпляр создан (эта строка должна появиться один раз)");
    }

    public static TokenLedger Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (Gate)
                {
                    _instance ??= new TokenLedger(); 
                }
            }

            return _instance;
        }
    }

    public void Add(int tokens) => Interlocked.Add(ref _totalTokens, tokens);

    public long TotalTokens => Volatile.Read(ref _totalTokens);
}

public sealed class LazyLedger
{
    private static readonly Lazy<LazyLedger> InstanceField = new(() => new LazyLedger());

    public static LazyLedger Instance => InstanceField.Value;

    private LazyLedger() { }
}

public sealed class UserSession
{
    private readonly string _user;

    public UserSession(string user) => _user = user;

    public long Run(int requestCount)
    {
        var rng = new Random(_user.GetHashCode());
        long expected = 0;

        for (int i = 0; i < requestCount; i++)
        {
            var tokens = rng.Next(10, 200);
            Thread.Sleep(1); 

            TokenLedger.Instance.Add(tokens);
            expected += tokens;
        }

        return expected;
    }
}

public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== Единственность экземпляра ===");
        var a = TokenLedger.Instance;
        var b = TokenLedger.Instance;
        Console.WriteLine($"ReferenceEquals(a, b) = {ReferenceEquals(a, b)}");
        Console.WriteLine($"Создано экземпляров: {TokenLedger.CreatedCount}");

        var users = new[] { "alice", "bob", "carol", "dave", "erin", "frank", "grace", "heidi" };
        var expected = new long[users.Length];
        var threads = new Thread[users.Length];

        for (int i = 0; i < users.Length; i++)
        {
            int idx = i;
            var session = new UserSession(users[idx]);
            threads[idx] = new Thread(() => expected[idx] = session.Run(20));
            threads[idx].Start();
        }

        foreach (var t in threads)
        {
            t.Join();
        }

        long expectedTotal = expected.Sum();
        long actualTotal = TokenLedger.Instance.TotalTokens;

        Console.WriteLine($"\nОжидалось токенов:    {expectedTotal}");
        Console.WriteLine($"Фактически в реестре: {actualTotal}");
        Console.WriteLine($"Создано экземпляров:  {TokenLedger.CreatedCount}");
        Console.WriteLine(expectedTotal == actualTotal && TokenLedger.CreatedCount == 1
            ? "OK: экземпляр один, ни один токен не потерян и не задвоен"
            : "РАСХОЖДЕНИЕ! Гонка при создании или за общее состояние");
    }
}
