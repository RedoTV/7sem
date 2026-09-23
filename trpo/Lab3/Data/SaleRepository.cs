// Уровень доступа к данным, операции над записями: добавление, изменение,
// удаление, поиск и выборка за период. Формирует SQL и обращается к Database,
// сама база данных этому модулю не видна.

using Lab3.Model;
using Microsoft.Data.Sqlite;

namespace Lab3.Data;

public static class SaleRepository
{
    public static List<Sale> GetAll()
    {
        return Database.Query("SELECT * FROM Sales ORDER BY Id", MapSale);
    }

    // Поиск по наименованию, артикулу или номеру чека.
    // Сравнение в C#: SQLite LIKE не приводит регистр кириллицы.
    public static List<Sale> Search(string text)
    {
        string query = text.Trim();
        List<Sale> sales = GetAll();

        if (query.Length == 0)
            return sales;

        return sales.FindAll(sale =>
            sale.Product.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            sale.Article.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            sale.Receipt.ToString().Contains(query, StringComparison.Ordinal));
    }

    // Выбирает продажи за период [from; to], даты в формате yyyy-MM-dd.
    public static List<Sale> GetForPeriod(string from, string to)
    {
        return Database.Query("SELECT * FROM Sales WHERE SaleDate BETWEEN @from AND @to ORDER BY SaleDate",
            MapSale, ("@from", from), ("@to", to));
    }

    public static double Total(List<Sale> sales)
    {
        double total = 0;

        foreach (Sale sale in sales)
            total += sale.Price * sale.Quantity;

        return total;
    }

    public static void Add(Sale sale)
    {
        Database.Execute("""
            INSERT INTO Sales (Shop, Section, Receipt, Product, Article, Price, Quantity, SaleDate)
            VALUES (@shop, @section, @receipt, @product, @article, @price, @quantity, @date)
            """,
            ("@shop", sale.Shop), ("@section", sale.Section), ("@receipt", sale.Receipt),
            ("@product", sale.Product), ("@article", sale.Article),
            ("@price", sale.Price), ("@quantity", sale.Quantity), ("@date", sale.Date));
    }

    public static void Update(Sale sale)
    {
        Database.Execute("""
            UPDATE Sales
            SET Shop = @shop, Section = @section, Receipt = @receipt, Product = @product,
                Article = @article, Price = @price, Quantity = @quantity, SaleDate = @date
            WHERE Id = @id
            """,
            ("@shop", sale.Shop), ("@section", sale.Section), ("@receipt", sale.Receipt),
            ("@product", sale.Product), ("@article", sale.Article),
            ("@price", sale.Price), ("@quantity", sale.Quantity), ("@date", sale.Date),
            ("@id", sale.Id));
    }

    public static void Delete(long id)
    {
        Database.Execute("DELETE FROM Sales WHERE Id = @id", ("@id", id));
    }

    // Заполняет базу записями при первом запуске (не менее 5 по условию).
    public static void SeedIfEmpty()
    {
        if (GetAll().Count > 0)
            return;

        // Две первые продажи старше 60 дней: отчет за период их не захватит.
        Add(Sale("Монитор LG 24MK430H", "LG-24MK", 1, 2, 100245, 12500, 3, 80));
        Add(Sale("Клавиатура Logitech K120", "LOG-K120", 1, 1, 100246, 990, 10, 70));
        Add(Sale("Наушники Sony WH-CH520", "SNY-CH520", 2, 3, 200118, 4350, 2, 40));
        Add(Sale("Мышь Logitech B100", "LOG-B100", 2, 1, 200119, 650, 15, 28));
        Add(Sale("SSD Kingston A400 480GB", "KIN-A400", 3, 2, 300512, 3200, 4, 16));
        Add(Sale("Кабель HDMI 2.0, 1.5 м", "HDMI-15", 1, 2, 100247, 450, 20, 9));
        Add(Sale("Веб-камера Defender G-eye", "DEF-1000", 3, 1, 300513, 1750, 5, 4));
        Add(Sale("Флешка SanDisk 64GB", "SDK-64", 2, 3, 200120, 890, 12, 1));
    }

    private static Sale Sale(string product, string article, int shop, int section,
        int receipt, double price, int quantity, int daysAgo)
    {
        return new Sale
        {
            Shop = shop,
            Section = section,
            Receipt = receipt,
            Product = product,
            Article = article,
            Price = price,
            Quantity = quantity,
            Date = DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd")
        };
    }

    // Читает текущую строку результата запроса в запись Sale.
    private static Sale MapSale(SqliteDataReader reader)
    {
        return new Sale
        {
            Id = reader.GetInt64(0),
            Shop = reader.GetInt32(1),
            Section = reader.GetInt32(2),
            Receipt = reader.GetInt32(3),
            Product = reader.GetString(4),
            Article = reader.GetString(5),
            Price = reader.GetDouble(6),
            Quantity = reader.GetInt32(7),
            Date = reader.GetString(8)
        };
    }
}
