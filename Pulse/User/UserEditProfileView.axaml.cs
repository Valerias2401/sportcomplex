using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class UserEditProfileView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public UserEditProfileView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public UserEditProfileView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _session = session;
        LoadUserData();
    }

    private async void LoadUserData()
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
                FullNameTextBox.Text = reader["full_name"]?.ToString() ?? "";
                EmailTextBox.Text = reader["email"]?.ToString() ?? "";
                PhoneTextBox.Text = reader["phone"]?.ToString() ?? "";
            }
        }
        catch (Exception ex)
        {
            ShowError("Ошибка загрузки данных: " + ex.Message);
        }
    }

    private async void SaveProfile(object? sender, RoutedEventArgs e)
    {
        HideMessage();

        string fullName = FullNameTextBox.Text?.Trim() ?? "";
        string email = EmailTextBox.Text?.Trim() ?? "";
        string phone = PhoneTextBox.Text?.Trim() ?? "";
        string password = PasswordTextBox.Text?.Trim() ?? "";

        string? error = ValidateForm(fullName, email, phone, password);

        if (error is not null)
        {
            ShowError(error);
            return;
        }

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            string sql;

            if (string.IsNullOrWhiteSpace(password))
            {
                sql = """
                    UPDATE users
                    SET full_name = @full_name,
                        email = @email,
                        phone = @phone
                    WHERE id_user = @id_user
                    """;
            }
            else
            {
                sql = """
                    UPDATE users
                    SET full_name = @full_name,
                        email = @email,
                        phone = @phone,
                        password = @password
                    WHERE id_user = @id_user
                    """;
            }

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@full_name", fullName);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@phone", phone);
            command.Parameters.AddWithValue("@id_user", _session.UserId);

            if (!string.IsNullOrWhiteSpace(password))
                command.Parameters.AddWithValue("@password", password);

            await command.ExecuteNonQueryAsync();

            _session.FullName = fullName;
            _session.Email = email;
            SessionService.SaveSession(_session);

            _mainWindow?.OpenUserProfile();
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            ShowError("Пользователь с такой почтой уже существует.");
        }
        catch (Exception ex)
        {
            ShowError("Ошибка сохранения: " + ex.Message);
        }
    }

    private string? ValidateForm(string fullName, string email, string phone, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "ФИО не может быть пустым.";

        if (string.IsNullOrWhiteSpace(email))
            return "Email не может быть пустым.";

        if (!email.Contains("@") || email.StartsWith("@") || email.EndsWith("@"))
            return "Введите корректный email.";

        if (string.IsNullOrWhiteSpace(phone))
            return "Телефон не может быть пустым.";

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
            return "Пароль должен содержать минимум 6 символов.";

        return null;
    }

    private void CancelEdit(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserProfile();
    }

    private void ShowError(string message)
    {
        MessageBorder.Background = Brush.Parse("#33D92525");
        MessageBorder.BorderBrush = Brush.Parse("#D92525");
        MessageText.Foreground = Brush.Parse("#D92525");
        MessageText.Text = message;
        MessageBorder.IsVisible = true;
    }

    private void HideMessage()
    {
        MessageBorder.IsVisible = false;
        MessageText.Text = "";
    }
}