// Запись БД «Реализованный товар» - общая модель данных.
// Через объекты этого класса модули интерфейса обмениваются
// данными с модулем доступа к данным.

namespace Lab3.Model;

public class Sale
{
    public long Id { get; set; }
    public int Shop { get; set; }        // номер магазина
    public int Section { get; set; }     // номер секции
    public int Receipt { get; set; }     // номер чека
    public string Product { get; set; } = "";   // наименование товара
    public string Article { get; set; } = "";   // артикул
    public double Price { get; set; }    // цена товара
    public int Quantity { get; set; }    // количество
    public string Date { get; set; } = "";      // дата продажи (yyyy-MM-dd)
}
