using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class UserHomeView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public UserHomeView()
    {
        InitializeComponent();
        _session = new SessionData();
        FillDemoData();
    }

    public UserHomeView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _session = session;
        FillPageData();
        LoadHomeInfo();
    }

    private void FillPageData()
    {
        WelcomeText.Text = $"Добро пожаловать,\n{_session.FullName}!";
        DateText.Text = $"Сегодня {DateTime.Now:dd.MM.yyyy}";
        NearestTrainingTitleText.Text = "Пока нет ближайших занятий";
        NearestTrainingInfoText.Text = "Запишитесь на занятие через поиск занятий";
    }

    private void FillDemoData()
    {
        WelcomeText.Text = "Добро пожаловать!";
        DateText.Text = $"Сегодня {DateTime.Now:dd.MM.yyyy}";
        NearestTrainingTitleText.Text = "Пока нет ближайших занятий";
        NearestTrainingInfoText.Text = "Запишитесь на занятие через поиск занятий";
    }

    private async void LoadHomeInfo()
    {
        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string nearestSql = """
                SELECT 
                    u.name_uslugi,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    trainer.full_name AS trainer_name
                FROM zapisi z
                JOIN raspisanie r ON r.id_raspisanie = z.id_raspisanie
                JOIN uslugi u ON u.id_uslugi = r.id_uslugi
                LEFT JOIN users trainer ON trainer.id_user = r.id_trainer
                WHERE z.id_user = @id_user
                  AND z.status_zapisi = 'active'
                  AND r.date_training >= CURRENT_DATE
                ORDER BY r.date_training, r.time_start
                LIMIT 1
                """;

            await using var nearestCommand = new NpgsqlCommand(nearestSql, connection);
            nearestCommand.Parameters.AddWithValue("@id_user", _session.UserId);

            await using var nearestReader = await nearestCommand.ExecuteReaderAsync();

            if (await nearestReader.ReadAsync())
            {
                string name = nearestReader["name_uslugi"]?.ToString() ?? "Занятие";
                string trainer = nearestReader["trainer_name"]?.ToString() ?? "Тренер не указан";
                DateTime date = Convert.ToDateTime(nearestReader["date_training"]);
                TimeSpan start = (TimeSpan)nearestReader["time_start"];
                TimeSpan end = (TimeSpan)nearestReader["time_end"];

                NearestTrainingTitleText.Text = name;
                NearestTrainingInfoText.Text = $"Тренер: {trainer}\nДата: {date:dd.MM.yyyy}\nВремя: {start:hh\\:mm} — {end:hh\\:mm}";
            }

            await nearestReader.CloseAsync();

            const string abonementSql = """
                SELECT a.name_abonement, ua.end_date
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
                string abonementName = abonementReader["name_abonement"]?.ToString() ?? "";
                DateTime endDate = Convert.ToDateTime(abonementReader["end_date"]);

                int daysLeft = (endDate.Date - DateTime.Today).Days;
                if (daysLeft < 0)
                    daysLeft = 0;

                DateText.Text =
                    $"Сегодня {DateTime.Now:dd.MM.yyyy}\n" +
                    $"Абонемент: {abonementName}\n" +
                    $"До {endDate:dd.MM.yyyy} • {daysLeft} дн.";
            }
            else
            {
                DateText.Text =
                    $"Сегодня {DateTime.Now:dd.MM.yyyy}\n" +
                    "Абонемент: нет";
            }
        }
        catch
        {
            DateText.Text =
                $"Сегодня {DateTime.Now:dd.MM.yyyy}\n" +
                "Абонемент: ошибка загрузки";

            NearestTrainingTitleText.Text = "Пока нет ближайших занятий";
            NearestTrainingInfoText.Text = "Запишитесь на занятие через поиск занятий";
        }
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

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserProfile();
    }
}