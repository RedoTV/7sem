// Построение таблицы записей о продажах. Одна и та же таблица
// используется на вкладке записей и в обеих отчетных формах.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Lab3.Model;

namespace Lab3.Gui;

public static class SaleTable
{
    public static DataGrid Create()
    {
        var grid = new DataGrid
        {
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            CanUserSortColumns = true,
            Margin = new Thickness(0, 0, 0, 8)
        };

        AddColumn(grid, "Магазин", nameof(Sale.Shop), new DataGridLength(80));
        AddColumn(grid, "Секция", nameof(Sale.Section), new DataGridLength(70));
        AddColumn(grid, "Чек", nameof(Sale.Receipt), new DataGridLength(90));
        AddColumn(grid, "Наименование товара", nameof(Sale.Product), new DataGridLength(1, DataGridLengthUnitType.Star));
        AddColumn(grid, "Артикул", nameof(Sale.Article), new DataGridLength(110));
        AddColumn(grid, "Цена", nameof(Sale.Price), new DataGridLength(90), "{0:0.00}");
        AddColumn(grid, "Кол-во", nameof(Sale.Quantity), new DataGridLength(70));
        AddColumn(grid, "Дата продажи", nameof(Sale.Date), new DataGridLength(110));

        return grid;
    }

    private static void AddColumn(DataGrid grid, string header, string property, DataGridLength width,
        string? format = null)
    {
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(property) { StringFormat = format },
            Width = width
        });
    }
}
