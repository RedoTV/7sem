// Вкладка отчетов: поиск записей по наименованию, артикулу или номеру чека
// и отчет о продажах за период с подсчетом количества и суммы.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Lab3.Data;

namespace Lab3.Gui;

public class ReportsView : UserControl
{
    public ReportsView()
    {
        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto,*,Auto,Auto"),
            Margin = new Thickness(8)
        };

        // --- поиск ---
        var searchBox = new TextBox
        {
            Watermark = "наименование, артикул или номер чека",
            Width = 320,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var searchButton = new Button { Content = "Найти" };
        var searchInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var searchGrid = SaleTable.Create();

        searchButton.Click += (sender, e) =>
        {
            var found = SaleRepository.Search(searchBox.Text ?? "");
            searchGrid.ItemsSource = found;
            searchInfo.Text = $"Найдено записей: {found.Count}";
        };

        var searchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        searchRow.Children.Add(searchBox);
        searchRow.Children.Add(searchButton);
        searchRow.Children.Add(searchInfo);

        // --- отчет за период ---
        var fromDate = new DatePicker { SelectedDate = DateTimeOffset.Now.AddDays(-60) };
        var toDate = new DatePicker { SelectedDate = DateTimeOffset.Now };
        var periodButton = new Button { Content = "Сформировать" };
        var periodInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var periodGrid = SaleTable.Create();

        periodButton.Click += (sender, e) =>
        {
            var sales = SaleRepository.GetForPeriod(
                fromDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                toDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "");

            periodGrid.ItemsSource = sales;

            double total = 0;
            foreach (var sale in sales)
                total += sale.Price * sale.Quantity;

            periodInfo.Text = $"Продаж за период: {sales.Count}, сумма: {total:F2} руб.";
        };

        var periodRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        periodRow.Children.Add(new TextBlock { Text = "С", VerticalAlignment = VerticalAlignment.Center });
        periodRow.Children.Add(fromDate);
        periodRow.Children.Add(new TextBlock { Text = "по", VerticalAlignment = VerticalAlignment.Center });
        periodRow.Children.Add(toDate);
        periodRow.Children.Add(periodButton);
        periodRow.Children.Add(periodInfo);

        layout.Children.Add(searchRow);
        layout.Children.Add(searchGrid);
        layout.Children.Add(periodRow);
        layout.Children.Add(periodGrid);

        Grid.SetRow(searchRow, 0);
        Grid.SetRow(searchGrid, 1);
        Grid.SetRow(periodRow, 2);
        Grid.SetRow(periodGrid, 3);
        Grid.SetRow(periodInfo, 4);

        Content = layout;
    }
}
