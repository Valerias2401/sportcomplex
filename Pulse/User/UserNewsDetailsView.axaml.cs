using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Pulse.Core;

namespace Pulse.User;

public partial class UserNewsDetailsView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public UserNewsDetailsView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public UserNewsDetailsView(MainWindow mainWindow, SessionData session, string title)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        LoadNews(title);
    }

    private void LoadNews(string title)
    {
        TitleText.Text = title;

        switch (title)
        {
            case "Новые тренажёры в зале":
                DateText.Text = "24.04.2026";
                ContentText.Text = "Мы обновили оборудование для силовых тренировок. Теперь тренироваться стало ещё удобнее и безопаснее. В зале появились новые тренажёры для разных групп мышц, чтобы каждый клиент мог подобрать нагрузку под свой уровень подготовки.";
                SetImage("avares://Pulse/Assets/Images/новые.png");
                break;

            case "Акция на абонементы":
                DateText.Text = "23.04.2026";
                ContentText.Text = "При покупке абонемента начисляется кэшбек на баланс клиента. Его можно использовать для оплаты услуг клуба. Это удобная возможность выгодно продлить тренировки и получить дополнительные бонусы.";
                SetImage("avares://Pulse/Assets/Images/абонемент.png");
                break;

            case "Конкурс среди клиентов":
                DateText.Text = "22.04.2026";
                ContentText.Text = "Участвуй в клубном челлендже, выполняй тренировки и получай бонусы на баланс. Самые активные клиенты смогут получить приятные призы и дополнительную мотивацию для тренировок.";
                SetImage("avares://Pulse/Assets/Images/кубок.png");
                break;

            case "Обновлены раздевалки":
                DateText.Text = "21.04.2026";
                ContentText.Text = "Мы сделали зону раздевалок удобнее: появились новые шкафчики, зеркала и места для хранения. Теперь подготовка к тренировке и отдых после занятия стали комфортнее.";
                SetImage("avares://Pulse/Assets/Images/раздевалка.png");
                break;

            case "Новое расписание йоги":
                DateText.Text = "20.04.2026";
                ContentText.Text = "Добавили дополнительные вечерние занятия по йоге. Теперь можно выбрать более удобное время для тренировки после учёбы, работы или других дел.";
                SetImage("avares://Pulse/Assets/Images/йога.png");
                break;

            case "Персональные тренировки":
                DateText.Text = "19.04.2026";
                ContentText.Text = "Теперь можно приобрести разовые персональные тренировки. Тренер поможет подобрать нагрузку, следить за техникой выполнения упражнений и быстрее прийти к нужному результату.";
                SetImage("avares://Pulse/Assets/Images/тренер.png");
                break;

            default:
                DateText.Text = "Дата";
                ContentText.Text = "Текст новости";
                NewsImage.Source = null;
                break;
        }
    }

    private void SetImage(string path)
    {
        try
        {
            NewsImage.Source = new Bitmap(AssetLoader.Open(new Uri(path)));
        }
        catch
        {
            NewsImage.Source = null;
        }
    }

    private void BackToNews(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserNews();
    }
}