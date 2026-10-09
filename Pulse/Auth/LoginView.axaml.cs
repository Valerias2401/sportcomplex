using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;
using System;

namespace Pulse;

public partial class LoginView : UserControl
{
    private MainWindow? _mainWindow;

    public LoginView()
    {
        InitializeComponent();
    }

    public LoginView(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void BackToMain(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.ShowMainMenu();
    }

    private async void LoginUser(object? sender, RoutedEventArgs e)
    {
        HideMessages();

        string email = EmailTextBox.Text?.Trim() ?? "";
        string password = PasswordTextBox.Text?.Trim() ?? "";

        string? error = ValidateLoginForm(email, password);

        if (error is not null)
        {
            ShowError(error);
            return;
        }

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT id_user, full_name, email, password, id_role
                FROM users
                WHERE email = @email
                LIMIT 1
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@email", email);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                ShowError("Пользователь с таким email не найден.");
                return;
            }

            string dbPassword = reader["password"]?.ToString() ?? "";

            if (dbPassword != password)
            {
                ShowError("Неверный пароль.");
                return;
            }

            var session = new SessionData
            {
                UserId = Convert.ToInt32(reader["id_user"]),
                FullName = reader["full_name"]?.ToString() ?? "Пользователь",
                Email = reader["email"]?.ToString() ?? "",
                RoleId = Convert.ToInt32(reader["id_role"])
            };

            SessionService.SaveSession(session);

            ShowSuccess($"Добро пожаловать, {session.FullName}!");

            _mainWindow?.OpenByRole(session);
        }
        catch (Exception ex)
        {
            ShowError("Ошибка при входе: " + ex.Message);
        }
    }

    private string? ValidateLoginForm(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email не может быть пустым.";

        if (!email.Contains("@"))
            return "Email должен содержать символ @.";

        if (email.StartsWith("@") || email.EndsWith("@"))
            return "Введите корректный email.";

        if (string.IsNullOrWhiteSpace(password))
            return "Пароль не может быть пустым.";

        if (password.Length < 6)
            return "Пароль должен содержать минимум 6 символов.";

        return null;
    }

    private void ShowError(string message)
    {
        SuccessBorder.IsVisible = false;
        SuccessText.Text = string.Empty;

        ErrorBorder.IsVisible = true;
        ErrorText.Text = message;
    }

    private void ShowSuccess(string message)
    {
        ErrorBorder.IsVisible = false;
        ErrorText.Text = string.Empty;

        SuccessBorder.IsVisible = true;
        SuccessText.Text = message;
    }

    private void HideMessages()
    {
        ErrorBorder.IsVisible = false;
        ErrorText.Text = string.Empty;

        SuccessBorder.IsVisible = false;
        SuccessText.Text = string.Empty;
    }
}