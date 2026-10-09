using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class UserProfileView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public UserProfileView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public UserProfileView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _session = session;
        LoadProfile();
    }

    private async void LoadProfile()
    {
        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string userSql = """
                SELECT full_name, email, phone, balance
                FROM users
                WHERE id_user = @id_user
                LIMIT 1
                """;

            await using var userCommand = new NpgsqlCommand(userSql, connection);
            userCommand.Parameters.AddWithValue("@id_user", _session.UserId);

            await using var userReader = await userCommand.ExecuteReaderAsync();

            if (await userReader.ReadAsync())
            {
                string fullName = userReader["full_name"]?.ToString() ?? "";
                string email = userReader["email"]?.ToString() ?? "";
                string phone = userReader["phone"]?.ToString() ?? "Не указан";
                decimal balance = Convert.ToDecimal(userReader["balance"]);

                FullNameText.Text = fullName;
                EmailText.Text = $"Email: {email}";
                PhoneText.Text = $"Телефон: {phone}";
                BalanceText.Text = $"Баланс: {balance:0.00} ₽";
            }

            await userReader.CloseAsync();

            const string abonementSql = """
                SELECT a.name_abonement, ua.start_date, ua.end_date
                FROM user_abonements ua
                JOIN abonements a ON a.id_abonement = ua.id_abonement
                WHERE ua.id_user = @id_user
                  AND ua.status_abonement = 'active'
                  AND ua.end_date >= CURRENT_DATE
                ORDER BY ua.end_date DESC
                LIMIT 1
                """;

            await using var abonementCommand = new NpgsqlCommand(abonementSql, connection);
            abonementCommand.Parameters.AddWithValue("@id_user", _session.UserId);

            await using var abonementReader = await abonementCommand.ExecuteReaderAsync();

            if (await abonementReader.ReadAsync())
            {
                string name = abonementReader["name_abonement"]?.ToString() ?? "";
                DateTime startDate = Convert.ToDateTime(abonementReader["start_date"]);
                DateTime endDate = Convert.ToDateTime(abonementReader["end_date"]);

                AbonementText.Text = $"{name}, с {startDate:dd.MM.yyyy} до {endDate:dd.MM.yyyy}";
            }
            else
            {
                AbonementText.Text = "Нет активного абонемента";
            }
        }
        catch (Exception ex)
        {
            AbonementText.Text = "Ошибка загрузки профиля: " + ex.Message;
        }
    }

    private void BuyAbonement(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserNews();
    }

    private void OpenEditProfile(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenEditProfile();
    }

    private void Logout(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.LogoutUser();
    }

    private void OpenMainClubPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserMainClubPage();
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserSchedule();
    }

    private void OpenNewsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserNews();
    }

    private void OpenMyTrainingsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserMyTrainings();
    }
}