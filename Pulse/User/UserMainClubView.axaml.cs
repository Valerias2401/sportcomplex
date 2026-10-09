using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Pulse.Core;

namespace Pulse.User;

public partial class UserMainClubView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    private readonly ZoneSlide[] _zoneSlides =
    {
        new ZoneSlide
        {
            Title = "Тренажёрный зал",
            ImagePath = "avares://Pulse/Assets/Images/gym_red.png",
            AdvantagesTitle = "Преимущества",
            Advantages = new[]
            {
                "• Современные силовые тренажёры",
                "• Зона свободных весов",
                "• Удобное пространство для тренировок",
                "• Комфортная атмосфера для прогресса"
            }
        },
        new ZoneSlide
        {
            Title = "Душевые и раздевалки",
            ImagePath = "avares://Pulse/Assets/Images/showers.png",
            AdvantagesTitle = "Преимущества",
            Advantages = new[]
            {
                "• Чистые и удобные душевые зоны",
                "• Индивидуальные шкафчики для вещей",
                "• Большие зеркала",
                "• Хорошее освещение"
            }
        },
        new ZoneSlide
        {
            Title = "Парковка",
            ImagePath = "avares://Pulse/Assets/Images/parking.png",
            AdvantagesTitle = "Преимущества",
            Advantages = new[]
            {
                "• Бесплатная парковка",
                "• Рядом со зданием",
                "• Экономия времени перед занятием",
                "• Комфортно для клиентов с плотным графиком"
            }
        },
        new ZoneSlide
        {
            Title = "Зал групповых занятий",
            ImagePath = "avares://Pulse/Assets/Images/group_room.png",
            AdvantagesTitle = "Преимущества",
            Advantages = new[]
            {
                "• Пространство для йоги, пилатеса и растяжки",
                "• Удобное покрытие для тренировок",
                "• Комфортная атмосфера для занятий в группе",
                "• Подходит для разных направлений фитнеса"
            }
        }
    };

    private readonly TrainerInfo[] _trainers =
    {
        new TrainerInfo
        {
            Name = "Анна Смирнова",
            Role = "Персональный тренер",
            ImagePath = "avares://Pulse/Assets/Images/тренер2.png",
            Experience = "Опыт работы: 6 лет",
            Price = "Средняя цена тренировки: 1 500 ₽",
            Credo = "Кредо: маленький прогресс каждый день даёт большой результат.",
            Description = "Помогает клиентам безопасно снижать вес, улучшать технику упражнений и становиться увереннее в зале."
        },
        new TrainerInfo
        {
            Name = "Максим Орлов",
            Role = "Тренер групповых программ",
            ImagePath = "avares://Pulse/Assets/Images/тренер1.png",
            Experience = "Опыт работы: 5 лет",
            Price = "Средняя цена тренировки: 1 300 ₽",
            Credo = "Кредо: тренировка должна заряжать энергией.",
            Description = "Проводит функциональные занятия, помогает развивать выносливость, силу и общую физическую форму."
        },
        new TrainerInfo
        {
            Name = "Екатерина Волкова",
            Role = "Персональный тренер",
            ImagePath = "avares://Pulse/Assets/Images/тренер4.png",
            Experience = "Опыт работы: 4 года",
            Price = "Средняя цена тренировки: 1 400 ₽",
            Credo = "Кредо: тело любит заботу, а не наказание.",
            Description = "Специализируется на растяжке, коррекции фигуры, мягком возвращении к тренировкам и работе с осанкой."
        },
        new TrainerInfo
        {
            Name = "Илья Кузнецов",
            Role = "Тренер тренажёрного зала",
            ImagePath = "avares://Pulse/Assets/Images/тренер3.png",
            Experience = "Опыт работы: 7 лет",
            Price = "Средняя цена тренировки: 1 700 ₽",
            Credo = "Кредо: правильная техника важнее большого веса.",
            Description = "Помогает набрать мышечную массу, поставить технику базовых упражнений и составить понятный план тренировок."
        },
        new TrainerInfo
        {
            Name = "Артём Михайлов",
            Role = "Тренер по йоге и пилатесу",
            ImagePath = "avares://Pulse/Assets/Images/тренер5.png",
            Experience = "Опыт работы: 8 лет",
            Price = "Средняя цена тренировки: 1 600 ₽",
            Credo = "Кредо: сильное тело начинается со спокойного движения.",
            Description = "Работает с гибкостью, осанкой, восстановлением после нагрузок и улучшением общего самочувствия."
        },
        new TrainerInfo
        {
            Name = "Даниил Морозов",
            Role = "Тренер по кардио",
            ImagePath = "avares://Pulse/Assets/Images/тренер6.png",
            Experience = "Опыт работы: 3 года",
            Price = "Средняя цена тренировки: 1 200 ₽",
            Credo = "Кредо: главное — не скорость, а регулярность.",
            Description = "Помогает развивать выносливость, улучшать работу сердца и делать кардио-тренировки интересными."
        }
    };

    private int _zoneIndex;
    private int _selectedTrainerIndex;

    public UserMainClubView()
    {
        InitializeComponent();
        _session = new SessionData();
        UpdateZoneSlide();
    }

    public UserMainClubView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _session = session;
        UpdateZoneSlide();
    }

    private void UpdateZoneSlide()
    {
        var slide = _zoneSlides[_zoneIndex];

        ZoneTitleText.Text = slide.Title;
        ZoneAdvantagesTitleText.Text = slide.AdvantagesTitle;

        Advantage1Text.Text = slide.Advantages[0];
        Advantage2Text.Text = slide.Advantages[1];
        Advantage3Text.Text = slide.Advantages[2];
        Advantage4Text.Text = slide.Advantages[3];

        Advantage5Text.Text = "";
        Advantage5Text.IsVisible = false;

        try
        {
            ZoneImage.Source = new Bitmap(AssetLoader.Open(new Uri(slide.ImagePath)));
        }
        catch
        {
            ZoneImage.Source = null;
        }
    }

    private void PrevZoneSlide(object? sender, RoutedEventArgs e)
    {
        _zoneIndex--;

        if (_zoneIndex < 0)
            _zoneIndex = _zoneSlides.Length - 1;

        UpdateZoneSlide();
    }

    private void NextZoneSlide(object? sender, RoutedEventArgs e)
    {
        _zoneIndex++;

        if (_zoneIndex >= _zoneSlides.Length)
            _zoneIndex = 0;

        UpdateZoneSlide();
    }

    private void OpenTrainer1(object? sender, PointerPressedEventArgs e)
    {
        ShowTrainerInfo(0);
    }

    private void OpenTrainer2(object? sender, PointerPressedEventArgs e)
    {
        ShowTrainerInfo(1);
    }

    private void OpenTrainer3(object? sender, PointerPressedEventArgs e)
    {
        ShowTrainerInfo(2);
    }

    private void OpenTrainer4(object? sender, PointerPressedEventArgs e)
    {
        ShowTrainerInfo(3);
    }

    private void OpenTrainer5(object? sender, PointerPressedEventArgs e)
    {
        ShowTrainerInfo(4);
    }

    private void OpenTrainer6(object? sender, PointerPressedEventArgs e)
    {
        ShowTrainerInfo(5);
    }

    private void ShowTrainerInfo(int index)
    {
        _selectedTrainerIndex = index;

        var trainer = _trainers[index];

        TrainerModalName.Text = trainer.Name;
        TrainerModalRole.Text = trainer.Role;
        TrainerModalExperience.Text = trainer.Experience;
        TrainerModalPrice.Text = trainer.Price;
        TrainerModalCredo.Text = trainer.Credo;
        TrainerModalDescription.Text = trainer.Description;

        try
        {
            TrainerModalImage.Source = new Bitmap(AssetLoader.Open(new Uri(trainer.ImagePath)));
        }
        catch
        {
            TrainerModalImage.Source = null;
        }

        TrainerInfoPanel.IsVisible = true;
    }

    private void CloseTrainerInfo(object? sender, RoutedEventArgs e)
    {
        TrainerInfoPanel.IsVisible = false;
    }

    private void BuyTraining(object? sender, RoutedEventArgs e)
    {
        var trainer = _trainers[_selectedTrainerIndex];

        TrainerInfoPanel.IsVisible = false;

        _mainWindow?.OpenUserSchedule();

        Debug.WriteLine($"Покупка тренировки у тренера: {trainer.Name}");
    }

    private void OpenRoute(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://yandex.ru/maps/?text=Казань%2C%20ул.%20Баумана%2C%2058А%2C%20фитнес-клуб%20Пульс");
    }

    private void OpenVk(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://vk.com");
    }

    private void OpenTelegram(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://t.me");
    }

    private void OpenMax(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://max.ru");
    }

    private void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
        }
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

    private class ZoneSlide
    {
        public string Title { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string AdvantagesTitle { get; set; } = "";
        public string[] Advantages { get; set; } = Array.Empty<string>();
    }

    private class TrainerInfo
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string Experience { get; set; } = "";
        public string Price { get; set; } = "";
        public string Credo { get; set; } = "";
        public string Description { get; set; } = "";
    }
}