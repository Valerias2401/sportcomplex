using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class AdminProfileView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public AdminProfileView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public AdminProfileView(MainWindow mainWindow, SessionData session)
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
                SELECT full_name, email, phone, id_role
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
                int roleId = Convert.ToInt32(reader["id_role"]);

                FullNameText.Text = fullName;
                EmailText.Text = $"Email: {email}";
                PhoneText.Text = $"Телефон: {phone}";
                RoleText.Text = $"Роль: {GetRoleName(roleId)}";
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

    private string GetRoleName(int roleId)
    {
        return roleId switch
        {
            1 => "администратор",
            2 => "тренер",
            3 => "посетитель",
            _ => "неизвестно"
        };
    }

    private void Logout(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.LogoutUser();
    }

    private void OpenHome(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminHome();
    }

    private void OpenSchedule(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminSchedule();
    }

    private void OpenTimes(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminTrainerTimes();
    }

    private void OpenClients(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminClients();
    }
}