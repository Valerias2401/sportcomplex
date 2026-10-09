using System;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;

namespace Pulse;

public partial class RegisterView : UserControl
{
    private MainWindow? _mainWindow;

    public RegisterView()
    {
        InitializeComponent();
    }

    public RegisterView(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void BackToMain(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.ShowMainMenu();
    }

    private async void RegisterUser(object? sender, RoutedEventArgs e)
    {
        HideMessages();

        string name = NameTextBox.Text?.Trim() ?? "";
        string email = EmailTextBox.Text?.Trim() ?? "";
        string password = PasswordTextBox.Text?.Trim() ?? "";
        string phone = PhoneTextBox.Text?.Trim() ?? "";
        DateTimeOffset? birthDate = BirthDatePicker.SelectedDate;

        string? error = ValidateRegisterForm(name, email, password, birthDate, phone);

        if (error is not null)
        {
            ShowError(error);
            return;
        }

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string checkEmailSql = """
                SELECT COUNT(*)
                FROM users
                WHERE email = @email
                """;

            await using (var checkCommand = new NpgsqlCommand(checkEmailSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@email", email);

                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (count > 0)
                {
                    ShowError("Пользователь с таким email уже существует.");
                    return;
                }
            }

            const string getRoleSql = """
                SELECT id_role
                FROM roles
                WHERE name_role = 'posetitel'
                LIMIT 1
                """;

            int roleId;

            await using (var roleCommand = new NpgsqlCommand(getRoleSql, connection))
            {
                var roleResult = await roleCommand.ExecuteScalarAsync();

                if (roleResult is null)
                {
                    ShowError("В базе не найдена роль posetitel.");
                    return;
                }

                roleId = Convert.ToInt32(roleResult);
            }

            const string insertUserSql = """
                INSERT INTO users (full_name, email, password, birth_date, phone, id_role)
                VALUES (@full_name, @email, @password, @birth_date, @phone, @id_role)
                """;

            await using (var insertCommand = new NpgsqlCommand(insertUserSql, connection))
            {
                insertCommand.Parameters.AddWithValue("@full_name", name);
                insertCommand.Parameters.AddWithValue("@email", email);
                insertCommand.Parameters.AddWithValue("@password", password);
                insertCommand.Parameters.AddWithValue("@birth_date", birthDate!.Value.Date);
                insertCommand.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
                insertCommand.Parameters.AddWithValue("@id_role", roleId);

                await insertCommand.ExecuteNonQueryAsync();
            }

            ShowSuccess("Вы успешно зарегистрировались.");

            NameTextBox.Text = string.Empty;
            EmailTextBox.Text = string.Empty;
            PasswordTextBox.Text = string.Empty;
            PhoneTextBox.Text = string.Empty;
            BirthDatePicker.SelectedDate = null;
        }
        catch (Exception ex)
        {
            ShowError("Ошибка при регистрации: " + ex.Message);
        }
    }

    private string? ValidateRegisterForm(
        string name,
        string email,
        string password,
        DateTimeOffset? birthDate,
        string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Имя не может быть пустым.";

        if (name.Length < 2)
            return "Имя должно содержать минимум 2 символа.";

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

        if (birthDate == null)
            return "Выберите дату рождения.";

        if (birthDate.Value.Date > DateTimeOffset.Now.Date)
            return "Дата рождения не может быть в будущем.";

        if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^\d{11}$"))
            return "Телефон должен содержать ровно 11 цифр.";

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