using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class UserNewsView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public static double SavedScrollOffset = 0;

    private readonly List<AbonementItem> _abonements = new();
    private AbonementItem? _selectedAbonement;

    public UserNewsView()
    {
        InitializeComponent();
        _session = new SessionData();

        RestoreScrollPosition();
    }

    public UserNewsView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        RestoreScrollPosition();
        _ = LoadAbonementsAsync();
    }

    private void RestoreScrollPosition()
    {
        AttachedToVisualTree += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                NewsScrollViewer.Offset = new Vector(0, SavedScrollOffset);
            }, DispatcherPriority.Loaded);
        };
    }

    private void OpenNewsDetails(object? sender, PointerPressedEventArgs e)
    {
        SavedScrollOffset = NewsScrollViewer.Offset.Y;

        if (sender is Border border && border.Tag is string title)
        {
            _mainWindow?.OpenNewsDetails(title);
        }
    }

    private async Task LoadAbonementsAsync()
    {
        try
        {
            _abonements.Clear();
            AbonementComboBox.Items.Clear();

            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT id_abonement, name_abonement, price_abonement, duration_days
                FROM abonements
                ORDER BY id_abonement
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var abonement = new AbonementItem
                {
                    Id = Convert.ToInt32(reader["id_abonement"]),
                    Name = reader["name_abonement"]?.ToString() ?? "",
                    Price = Convert.ToDecimal(reader["price_abonement"]),
                    DurationDays = Convert.ToInt32(reader["duration_days"])
                };

                _abonements.Add(abonement);
                AbonementComboBox.Items.Add(abonement);
            }
        }
        catch (Exception ex)
        {
            ShowPurchaseMessage("Ошибка загрузки абонементов: " + ex.Message);
        }
    }

    private void OpenPurchasePanel(object? sender, RoutedEventArgs e)
    {
        PurchaseMessageBorder.IsVisible = false;

        if (sender is Button button && button.Tag is string tag && int.TryParse(tag, out int abonementId))
        {
            _selectedAbonement = _abonements.Find(x => x.Id == abonementId);

            if (_selectedAbonement != null)
            {
                AbonementComboBox.SelectedItem = _selectedAbonement;
                UpdatePurchaseInfo();
            }
        }

        PurchaseOverlay.IsVisible = true;
    }

    private void ClosePurchasePanel(object? sender, RoutedEventArgs e)
    {
        PurchaseOverlay.IsVisible = false;
        PurchaseMessageBorder.IsVisible = false;
    }

    private void AbonementChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AbonementComboBox.SelectedItem is AbonementItem abonement)
        {
            _selectedAbonement = abonement;
            UpdatePurchaseInfo();
        }
    }

    private void UpdatePurchaseInfo()
    {
        if (_selectedAbonement == null)
            return;

        decimal price = _selectedAbonement.Price;
        decimal discount = Math.Round(price * 0.03m, 2);
        decimal total = price - discount;
        decimal cashback = Math.Round(total * 0.07m, 2);

        PurchaseDurationText.Text = $"{_selectedAbonement.DurationDays} дней";
        PurchasePriceText.Text = $"{price:0.00} ₽";
        PurchaseDiscountText.Text = $"{discount:0.00} ₽";
        PurchaseCashbackText.Text = $"{cashback:0.00} ₽";
        PurchaseTotalText.Text = $"{total:0.00} ₽";
    }

    private async void BuySelectedAbonement(object? sender, RoutedEventArgs e)
    {
        if (_selectedAbonement == null)
        {
            ShowPurchaseMessage("Выберите абонемент.");
            return;
        }

        try
        {
            decimal price = _selectedAbonement.Price;
            decimal discount = Math.Round(price * 0.03m, 2);
            decimal total = price - discount;
            decimal cashback = Math.Round(total * 0.07m, 2);

            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            const string balanceSql = """
                SELECT balance
                FROM users
                WHERE id_user = @id_user
                LIMIT 1
                """;

            await using var balanceCommand = new NpgsqlCommand(balanceSql, connection, transaction);
            balanceCommand.Parameters.AddWithValue("@id_user", _session.UserId);

            var balanceObject = await balanceCommand.ExecuteScalarAsync();
            decimal currentBalance = balanceObject == null ? 0 : Convert.ToDecimal(balanceObject);

            if (currentBalance < total)
            {
                await transaction.RollbackAsync();
                ShowPurchaseMessage("Недостаточно средств на карте клиента.");
                return;
            }

            const string updateBalanceSql = """
                UPDATE users
                SET balance = balance - @total + @cashback
                WHERE id_user = @id_user
                """;

            await using var updateBalanceCommand = new NpgsqlCommand(updateBalanceSql, connection, transaction);
            updateBalanceCommand.Parameters.AddWithValue("@total", total);
            updateBalanceCommand.Parameters.AddWithValue("@cashback", cashback);
            updateBalanceCommand.Parameters.AddWithValue("@id_user", _session.UserId);
            await updateBalanceCommand.ExecuteNonQueryAsync();

            DateTime startDate = DateTime.Today;
            DateTime endDate = startDate.AddDays(_selectedAbonement.DurationDays);

            const string deactivateSql = """
                UPDATE user_abonements
                SET status_abonement = 'inactive'
                WHERE id_user = @id_user
                  AND status_abonement = 'active'
                """;

            await using var deactivateCommand = new NpgsqlCommand(deactivateSql, connection, transaction);
            deactivateCommand.Parameters.AddWithValue("@id_user", _session.UserId);
            await deactivateCommand.ExecuteNonQueryAsync();

            const string insertSql = """
                INSERT INTO user_abonements
                (id_user, id_abonement, start_date, end_date, status_abonement)
                VALUES
                (@id_user, @id_abonement, @start_date, @end_date, 'active')
                """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("@id_user", _session.UserId);
            insertCommand.Parameters.AddWithValue("@id_abonement", _selectedAbonement.Id);
            insertCommand.Parameters.AddWithValue("@start_date", startDate);
            insertCommand.Parameters.AddWithValue("@end_date", endDate);
            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            PurchaseOverlay.IsVisible = false;
            PurchaseMessageBorder.IsVisible = false;
        }
        catch (Exception ex)
        {
            ShowPurchaseMessage("Ошибка покупки: " + ex.Message);
        }
    }

    private void ShowPurchaseMessage(string message)
    {
        PurchaseMessageText.Text = message;
        PurchaseMessageBorder.IsVisible = true;
    }

    private void OpenMainClubPage(object? sender, RoutedEventArgs e)
    {
        SavedScrollOffset = NewsScrollViewer.Offset.Y;
        _mainWindow?.OpenUserMainClubPage();
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        SavedScrollOffset = NewsScrollViewer.Offset.Y;
        _mainWindow?.OpenUserSchedule();
    }

    private void OpenMyTrainingsPage(object? sender, RoutedEventArgs e)
    {
        SavedScrollOffset = NewsScrollViewer.Offset.Y;
        _mainWindow?.OpenUserMyTrainings();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        SavedScrollOffset = NewsScrollViewer.Offset.Y;
        _mainWindow?.OpenUserProfile();
    }

    private class AbonementItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int DurationDays { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}