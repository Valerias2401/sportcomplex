using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class TrainerProfileView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public TrainerProfileView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public TrainerProfileView(MainWindow mainWindow, SessionData session)
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

            const string sql = """
                SELECT full_name, email, phone
                FROM users
                WHERE id_user = @id_user
                LIMIT 1
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id_user", _session.UserId);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                string fullName = reader["full_name"]?.ToString() ?? "";
                string email = reader["email"]?.ToString() ?? "";
                string phone = reader["phone"]?.ToString() ?? "Не указан";

                FullNameText.Text = fullName;
                EmailText.Text = $"Email: {email}";
                PhoneText.Text = $"Телефон: {phone}";
                RoleText.Text = "Роль: тренер";
            }
        }
        catch (Exception ex)
        {
            FullNameText.Text = "Ошибка загрузки профиля";
            EmailText.Text = ex.Message;
            PhoneText.Text = "";
            RoleText.Text = "";
        }
    }

    private void Logout(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.LogoutUser();
    }

    private void OpenHomePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerHome();
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerSchedule();
    }

    private void OpenClientsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerClients();
    }

    private void OpenAvailableTimePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerAvailableTime();
    }
}