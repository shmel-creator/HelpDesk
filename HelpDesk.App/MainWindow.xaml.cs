using System.Windows;
using HelpDesk.Core;
using HelpDesk.Data;
using Microsoft.Extensions.Configuration;

namespace HelpDesk.App;

public partial class MainWindow : Window
{
    private readonly TicketService ticketService;
    private readonly EfEmployeeRepository employeeRepo;
    private readonly EfEquipmentRepository equipmentRepo;

    public MainWindow()
    {
        InitializeComponent();

        // Читаем строку подключения из защищённого хранилища user-secrets
        var config = new ConfigurationBuilder()
            .AddUserSecrets<MainWindow>()
            .Build();

        string connectionString = config.GetConnectionString("HelpDesk");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("Не задана строка подключения ConnectionStrings:HelpDesk в user-secrets.", "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            throw new InvalidOperationException("Отсутствует строка подключения.");
        }

        // Инициализируем репозитории EF Core и сервис бизнес-логики
        var ticketRepo = new EfTicketRepository(connectionString);
        this.employeeRepo = new EfEmployeeRepository(connectionString);
        this.equipmentRepo = new EfEquipmentRepository(connectionString);

        this.ticketService = new TicketService(
            ticketRepo,
            this.employeeRepo,
            new StatusTransitionValidator(),
            new SlaCalculator());

        // Заполняем выпадающие списки значениями перечислений
        this.priorityBox.ItemsSource = Enum.GetValues(typeof(Priority));
        this.priorityBox.SelectedItem = Priority.Normal;

        this.statusBox.ItemsSource = Enum.GetValues(typeof(Status));
        this.statusBox.SelectedItem = Status.InProgress;

        LoadData();
    }

    // Загружает оборудование и заявки из БД в интерфейс
    private void LoadData()
    {
        try
        {
            var equipmentList = this.equipmentRepo.GetActive();
            this.equipmentBox.ItemsSource = equipmentList;
            if (equipmentList.Count > 0)
            {
                this.equipmentBox.SelectedIndex = 0;
            }

            var tickets = this.ticketService.GetTicketsWithSla();
            this.ticketsGrid.ItemsSource = tickets;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных из PostgreSQL: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Обработчик кнопки создания новой заявки
    private void OnCreateTicketClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedEquipment = this.equipmentBox.SelectedItem as Equipment;
            if (selectedEquipment == null)
            {
                MessageBox.Show("Выберите оборудование.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var employees = this.employeeRepo.GetActive();
            if (employees.Count == 0)
            {
                MessageBox.Show("В базе нет активных сотрудников.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ticket = new Ticket
            {
                AuthorId = employees[0].Id,
                EquipmentId = selectedEquipment.Id,
                Title = this.titleBox.Text,
                Description = this.descriptionBox.Text,
                Priority = (Priority)this.priorityBox.SelectedItem
            };

            this.ticketService.CreateTicket(ticket);
            this.titleBox.Clear();
            this.descriptionBox.Clear();
            LoadData();
        }
        catch (DomainException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Обработчик кнопки смены статуса выбранной заявки
    private void OnChangeStatusClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedTicket = this.ticketsGrid.SelectedItem as Ticket;
            if (selectedTicket == null)
            {
                MessageBox.Show("Выберите заявку в таблице.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var employees = this.employeeRepo.GetActive();
            int changedById = employees.Count > 0 ? employees[0].Id : 1;
            var newStatus = (Status)this.statusBox.SelectedItem;

            this.ticketService.ChangeStatus(selectedTicket.Id, newStatus, changedById);
            LoadData();
        }
        catch (DomainException ex)
        {
            MessageBox.Show(ex.Message, "Недопустимое действие", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (NotFoundException ex)
        {
            MessageBox.Show(ex.Message, "Не найдено", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Обновляет список заявок по нажатию кнопки
    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        LoadData();
    }
}