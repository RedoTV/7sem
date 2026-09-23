// Форма добавления и редактирования. В базу не пишет: проверяет поля
// и возвращает запись, сохранить ее решает главное окно.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Lab3.Logic;
using Lab3.Model;

namespace Lab3.Gui;

public class EditWindow : Window
{
    private readonly Sale? _sale; // null - добавление новой записи

    private readonly TextBox _shop = new();
    private readonly TextBox _section = new();
    private readonly TextBox _receipt = new();
    private readonly TextBox _product = new();
    private readonly TextBox _article = new();
    private readonly TextBox _price = new();
    private readonly TextBox _quantity = new();
    private readonly DatePicker _date = new();
    private readonly TextBlock _error = new() { Foreground = Brushes.Red };

    public EditWindow(Sale? sale)
    {
        _sale = sale;

        Title = sale == null ? "Новая запись" : "Редактирование записи";
        Width = 360;
        Height = 460;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var layout = new Grid
        {
            Margin = new Thickness(16),
            ColumnDefinitions = new ColumnDefinitions("Auto,220"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto")
        };

        AddField(layout, 0, "Магазин:", _shop);
        AddField(layout, 1, "Секция:", _section);
        AddField(layout, 2, "Чек:", _receipt);
        AddField(layout, 3, "Наименование:", _product);
        AddField(layout, 4, "Артикул:", _article);
        AddField(layout, 5, "Цена:", _price);
        AddField(layout, 6, "Количество:", _quantity);
        AddField(layout, 7, "Дата продажи:", _date);
        AddField(layout, 8, "", _error);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var saveButton = new Button { Content = "Сохранить" };
        saveButton.Click += OnSave;
        var cancelButton = new Button { Content = "Отмена" };
        cancelButton.Click += (sender, e) => Close();

        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        AddField(layout, 9, "", buttons);

        Content = layout;

        if (sale != null)
        {
            _shop.Text = sale.Shop.ToString();
            _section.Text = sale.Section.ToString();
            _receipt.Text = sale.Receipt.ToString();
            _product.Text = sale.Product;
            _article.Text = sale.Article;
            _price.Text = sale.Price.ToString("F2");
            _quantity.Text = sale.Quantity.ToString();
            _date.SelectedDate = DateTimeOffset.Parse(sale.Date);
        }
        else
        {
            _date.SelectedDate = DateTimeOffset.Now;
        }
    }

    private void AddField(Grid layout, int row, string caption, Control control)
    {
        var label = new TextBlock
        {
            Text = caption,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 8, 4)
        };

        control.Margin = new Thickness(0, 4, 0, 4);

        Grid.SetRow(label, row);
        Grid.SetColumn(label, 0);
        Grid.SetRow(control, row);
        Grid.SetColumn(control, 1);

        layout.Children.Add(label);
        layout.Children.Add(control);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        string? error = SaleValidator.Parse(
            _shop.Text ?? "", _section.Text ?? "", _receipt.Text ?? "",
            _product.Text ?? "", _article.Text ?? "", _price.Text ?? "", _quantity.Text ?? "",
            _date.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
            out Sale sale);

        if (error != null)
        {
            _error.Text = error;
            return;
        }

        if (_sale != null)
            sale.Id = _sale.Id;

        Close(sale);
    }
}
