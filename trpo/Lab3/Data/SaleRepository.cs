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

    // Ищет по наименованию, артикулу или номеру чека.
    public static List<Sale> Search(string text)
    {
        return Database.Query("""
            SELECT * FROM Sales
            WHERE Product LIKE @q OR Article LIKE @q OR CAST(Receipt AS TEXT) LIKE @q
            ORDER BY Id
            """, MapSale, ("@q", "%" + text.Trim() + "%"));
    }

    // Выбирает продажи за период [from; to], даты в формате yyyy-MM-dd.
    public static List<Sale> GetForPeriod(string from, string to)
    {
        return Database.Query("SELECT * FROM Sales WHERE SaleDate BETWEEN @from AND @to ORDER BY SaleDate",
            MapSale, ("@from", from), ("@to", to));
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

        Add(new Sale { Shop = 1, Section = 2, Receipt = 100245, Product = "Монитор LG 24MK430H",      Article = "LG-24MK",  Price = 12500, Quantity = 3,  Date = "2025-01-14" });
        Add(new Sale { Shop = 1, Section = 1, Receipt = 100246, Product = "Клавиатура Logitech K120",  Article = "LOG-K120", Price = 990,   Quantity = 10, Date = "2025-01-21" });
        Add(new Sale { Shop = 2, Section = 3, Receipt = 200118, Product = "Наушники Sony WH-CH520",    Article = "SNY-CH520", Price = 4350, Quantity = 2,  Date = "2025-02-03" });
        Add(new Sale { Shop = 2, Section = 1, Receipt = 200119, Product = "Мышь Logitech B100",        Article = "LOG-B100", Price = 650,   Quantity = 15, Date = "2025-02-07" });
        Add(new Sale { Shop = 3, Section = 2, Receipt = 300512, Product = "SSD Kingston A400 480GB",   Article = "KIN-A400", Price = 3200,  Quantity = 4,  Date = "2025-02-19" });
        Add(new Sale { Shop = 1, Section = 2, Receipt = 100247, Product = "Кабель HDMI 2.0, 1.5 м",    Article = "HDMI-15",  Price = 450,   Quantity = 20, Date = "2025-02-26" });
        Add(new Sale { Shop = 3, Section = 1, Receipt = 300513, Product = "Веб-камера Defender G-eye", Article = "DEF-1000", Price = 1750,  Quantity = 5,  Date = "2025-03-05" });
        Add(new Sale { Shop = 2, Section = 3, Receipt = 200120, Product = "Флешка SanDisk 64GB",       Article = "SDK-64",   Price = 890,   Quantity = 12, Date = "2025-03-12" });
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
