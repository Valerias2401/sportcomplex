using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class AdminScheduleView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public AdminScheduleView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public AdminScheduleView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        TrainingDatePicker.SelectedDate = DateTimeOffset.Now;
        LoadPageData();
    }

    private async void LoadPageData()
    {
        await LoadServicesAsync();
        await LoadTrainersAsync();
        LoadSchedule();
    }

    private async System.Threading.Tasks.Task LoadServicesAsync()
    {
        ServiceComboBox.Items.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT id_uslugi, name_uslugi, price_uslugi
                FROM uslugi
                ORDER BY name_uslugi
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ServiceComboBox.Items.Add(new ComboBoxItem
                {
                    Content = reader["name_uslugi"]?.ToString() ?? "",
                    Tag = new ServiceItem
                    {
                        Id = Convert.ToInt32(reader["id_uslugi"]),
                        Name = reader["name_uslugi"]?.ToString() ?? "",
                        Price = Convert.ToDecimal(reader["price_uslugi"])
                    }
                });
            }

            if (ServiceComboBox.Items.Count > 0)
                ServiceComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка загрузки услуг: " + ex.Message, true);
        }
    }

    private async System.Threading.Tasks.Task LoadTrainersAsync()
    {
        TrainerComboBox.Items.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT id_user, full_name
                FROM users
                WHERE id_role = 2
                ORDER BY full_name
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                TrainerComboBox.Items.Add(new ComboBoxItem
                {
                    Content = reader["full_name"]?.ToString() ?? "",
                    Tag = Convert.ToInt32(reader["id_user"])
                });
            }

            if (TrainerComboBox.Items.Count > 0)
                TrainerComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка загрузки тренеров: " + ex.Message, true);
        }
    }

    private void ServiceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ServiceComboBox.SelectedItem is ComboBoxItem item &&
            item.Tag is ServiceItem service)
        {
            PriceTextBox.Text = service.Price.ToString("0.00");
        }
    }

    private async void LoadSchedule()
    {
        SchedulePanel.Children.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT
                    r.id_raspisanie,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people,
                    u.name_uslugi,
                    u.type_uslugi,
                    u.duration_minutes,
                    COALESCE(r.price_training, u.price_uslugi) AS price_training,
                    trainer.full_name AS trainer_name,
                    COALESCE(COUNT(z.id_zapisi), 0) AS busy_places
                FROM raspisanie r
                JOIN uslugi u ON u.id_uslugi = r.id_uslugi
                LEFT JOIN users trainer ON trainer.id_user = r.id_trainer
                LEFT JOIN zapisi z ON z.id_raspisanie = r.id_raspisanie
                              AND z.status_zapisi != 'cancelled'
                WHERE r.date_training >= CURRENT_DATE
                GROUP BY
                    r.id_raspisanie,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people,
                    u.name_uslugi,
                    u.type_uslugi,
                    u.duration_minutes,
                    r.price_training,
                    u.price_uslugi,
                    trainer.full_name
                ORDER BY r.date_training, r.time_start
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            bool hasRows = false;

            while (await reader.ReadAsync())
            {
                hasRows = true;

                string name = reader["name_uslugi"]?.ToString() ?? "Занятие";
                string type = reader["type_uslugi"]?.ToString() ?? "";
                string trainer = reader["trainer_name"]?.ToString() ?? "Тренер не указан";
                DateTime date = Convert.ToDateTime(reader["date_training"]);
                TimeSpan start = (TimeSpan)reader["time_start"];
                TimeSpan end = (TimeSpan)reader["time_end"];
                int duration = Convert.ToInt32(reader["duration_minutes"]);
                decimal price = Convert.ToDecimal(reader["price_training"]);
                int maxPeople = Convert.ToInt32(reader["max_people"]);
                int busyPlaces = Convert.ToInt32(reader["busy_places"]);

                AddScheduleCard(name, type, trainer, date, start, end, duration, price, maxPeople, busyPlaces);
            }

            if (!hasRows)
                AddEmptyCard();
        }
        catch (Exception ex)
        {
            AddErrorCard(ex.Message);
        }
    }

    private async void CreateTraining(object? sender, RoutedEventArgs e)
    {
        HideMessage();

        if (ServiceComboBox.SelectedItem is not ComboBoxItem serviceItem ||
            serviceItem.Tag is not ServiceItem service)
        {
            ShowMessage("Выберите услугу.", true);
            return;
        }

        if (TrainerComboBox.SelectedItem is not ComboBoxItem trainerItem)
        {
            ShowMessage("Выберите тренера.", true);
            return;
        }

        int trainerId = Convert.ToInt32(trainerItem.Tag);

        if (!TrainingDatePicker.SelectedDate.HasValue)
        {
            ShowMessage("Выберите дату занятия.", true);
            return;
        }

        DateTime date = TrainingDatePicker.SelectedDate.Value.DateTime.Date;

        if (date < DateTime.Today)
        {
            ShowMessage("Нельзя создать занятие на прошедшую дату.", true);
            return;
        }

        if (!TimeSpan.TryParse(StartTimeTextBox.Text?.Trim(), out TimeSpan start))
        {
            ShowMessage("Введите начало в формате 10:00.", true);
            return;
        }

        if (!TimeSpan.TryParse(EndTimeTextBox.Text?.Trim(), out TimeSpan end))
        {
            ShowMessage("Введите конец в формате 11:00.", true);
            return;
        }

        if (start >= end)
        {
            ShowMessage("Время начала должно быть раньше времени окончания.", true);
            return;
        }

        if (!decimal.TryParse(PriceTextBox.Text?.Trim(), out decimal price))
        {
            ShowMessage("Введите корректную цену.", true);
            return;
        }

        if (price < 0)
        {
            ShowMessage("Цена не может быть отрицательной.", true);
            return;
        }

        if (!int.TryParse(MaxPeopleTextBox.Text?.Trim(), out int maxPeople))
        {
            ShowMessage("Введите количество мест.", true);
            return;
        }

        if (maxPeople <= 0)
        {
            ShowMessage("Количество мест должно быть больше нуля.", true);
            return;
        }

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string checkSql = """
                SELECT COUNT(*)
                FROM raspisanie
                WHERE id_trainer = @id_trainer
                  AND date_training = @date_training
                  AND (
                        (@time_start >= time_start AND @time_start < time_end)
                     OR (@time_end > time_start AND @time_end <= time_end)
                     OR (@time_start <= time_start AND @time_end >= time_end)
                  )
                """;

            await using var checkCommand = new NpgsqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@id_trainer", trainerId);
            checkCommand.Parameters.AddWithValue("@date_training", date);
            checkCommand.Parameters.AddWithValue("@time_start", start);
            checkCommand.Parameters.AddWithValue("@time_end", end);

            int exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            if (exists > 0)
            {
                ShowMessage("У этого тренера уже есть занятие на это время.", true);
                return;
            }

            const string insertSql = """
                INSERT INTO raspisanie
                (id_uslugi, id_trainer, date_training, time_start, time_end, max_people, price_training)
                VALUES
                (@id_uslugi, @id_trainer, @date_training, @time_start, @time_end, @max_people, @price_training)
                """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@id_uslugi", service.Id);
            insertCommand.Parameters.AddWithValue("@id_trainer", trainerId);
            insertCommand.Parameters.AddWithValue("@date_training", date);
            insertCommand.Parameters.AddWithValue("@time_start", start);
            insertCommand.Parameters.AddWithValue("@time_end", end);
            insertCommand.Parameters.AddWithValue("@max_people", maxPeople);
            insertCommand.Parameters.AddWithValue("@price_training", price);

            await insertCommand.ExecuteNonQueryAsync();

            ShowMessage("Занятие успешно создано.", false);

            StartTimeTextBox.Text = "";
            EndTimeTextBox.Text = "";
            MaxPeopleTextBox.Text = "";

            LoadSchedule();
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка создания занятия: " + ex.Message, true);
        }
    }

    private void AddScheduleCard(
        string name,
        string type,
        string trainer,
        DateTime date,
        TimeSpan start,
        TimeSpan end,
        int duration,
        decimal price,
        int maxPeople,
        int busyPlaces)
    {
        int freePlaces = maxPeople - busyPlaces;
        if (freePlaces < 0)
            freePlaces = 0;

        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,240"),
            ColumnSpacing = 22
        };

        var left = new StackPanel { Spacing = 8 };

        left.Children.Add(new TextBlock
        {
            Text = name,
            Foreground = Brush.Parse("#141414"),
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = GetTypeName(type),
            Foreground = Brush.Parse("#E32626"),
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Тренер: {trainer}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Дата: {date:dd.MM.yyyy}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Время: {start:hh\\:mm} — {end:hh\\:mm}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Длительность: {duration} минут",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Цена: {price:0.00} ₽",
            Foreground = Brush.Parse("#141414"),
            FontSize = 21,
            FontWeight = FontWeight.Bold
        });

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new Border
        {
            Background = Brush.Parse("#F7F7F7"),
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(20),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Записано",
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"{busyPlaces} из {maxPeople}",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 34,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"Свободно: {freePlaces}",
                        Foreground = freePlaces > 0 ? Brush.Parse("#27AE60") : Brush.Parse("#E32626"),
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            }
        };

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        card.Child = grid;
        SchedulePanel.Children.Add(card);
    }

    private void AddEmptyCard()
    {
        SchedulePanel.Children.Add(new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Занятий пока нет",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = "Создайте первое занятие через форму выше.",
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });
    }

    private void AddErrorCard(string message)
    {
        SchedulePanel.Children.Add(new Border
        {
            Background = Brush.Parse("#FFF1F1"),
            BorderBrush = Brush.Parse("#E32626"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Ошибка загрузки расписания",
                        Foreground = Brush.Parse("#E32626"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = message,
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });
    }

    private string GetTypeName(string type)
    {
        return type switch
        {
            "group" => "Групповое занятие",
            "personal" => "Персональная тренировка",
            "recovery" => "Восстановление",
            _ => "Занятие"
        };
    }

    private void ShowMessage(string message, bool isError)
    {
        MessageText.Text = message;
        MessageBorder.IsVisible = true;

        if (isError)
        {
            MessageBorder.Background = Brush.Parse("#33D92525");
            MessageBorder.BorderBrush = Brush.Parse("#D92525");
            MessageText.Foreground = Brush.Parse("#D92525");
        }
        else
        {
            MessageBorder.Background = Brush.Parse("#3327AE60");
            MessageBorder.BorderBrush = Brush.Parse("#27AE60");
            MessageText.Foreground = Brush.Parse("#141414");
        }
    }

    private void HideMessage()
    {
        MessageBorder.IsVisible = false;
        MessageText.Text = "";
    }

    private void OpenHomePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminHome();
    }

    private void OpenTrainerTimesPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminTrainerTimes();
    }

    private void OpenClientsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminClients();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminProfile();
    }

    private class ServiceItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}