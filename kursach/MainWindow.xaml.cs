//sqlite как бд
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using static kursach.MainWindow;
using static kursach.Transaction;
namespace kursach;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>

public partial class MainWindow : Window
{
    
    public ObservableCollection<Budget> Budgets { get; set; } = new ObservableCollection<Budget>();

    public const string CONNECTION_STRING = "data source=user_data.db";


    public enum Category
    {
        Food = 0,       // Продукты питания и напитки
        Housing,        // Жилье (аренда, ипотека, коммунальные услуги)
        Utilities,      // Коммунальные услуги (электричество, вода, газ, интернет, телефон)
        Transportation, // Транспорт (бензин, общественный транспорт, ремонт авто)
        Health,         // Здоровье (медицинские услуги, лекарства, страхование)
        Education,      // Образование (книги, курсы, обучение)
        Clothing,       // Одежда и обувь
        Entertainment,  // Развлечения (кино, концерты, хобби)
        PersonalCare,   // Уход за собой (косметика, парикмахерские услуги)
        Gifts,          // Подарки и поздравления
        Insurance,      // Страхование (жизни, имущества)
        Investments,    // Инвестиции
        Savings,        // Накопления
        Others          // Прочие расходы
    }

    public MainWindow()
    {
        InitializeComponent();
        #region создание таблиц если их нет, заполнение списка бюджетов
#if true
        CreateTables();
        CreateBudgets();
#endif
        #endregion
        this.DataContext = this;
        t_cat_cmbbx.ItemsSource = Enum.GetValues(typeof(Category));
    }


    void CreateTables()
    { // создание таблиц с транзакциями и с бюджетами
        string query = @"
        CREATE TABLE IF NOT EXISTS transactions(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        b_id INT, 
        sum REAL,
        info TEXT,
        date TEXT,
        cat INT
        );
        ";
        string query2 = @"
        CREATE TABLE IF NOT EXISTS budgets(
        b_id INTEGER PRIMARY KEY AUTOINCREMENT, 
        name TEXT,
        sum REAL,
        budgetAmount REAL
        );
        ";
        using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
        {
            connection.Open();
            using (SqliteCommand command = new SqliteCommand(query, connection)) { command.ExecuteNonQuery(); }
            using (SqliteCommand command = new SqliteCommand(query2, connection)) { command.ExecuteNonQuery(); }
        }
    }
    void CreateBudgets()
    { // чтение бюджетов из бд
        string query = "SELECT b_id, name, sum, budgetAmount FROM budgets";
        using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
        {
            connection.Open();
            using (SqliteCommand command = new SqliteCommand(query, connection))
            {
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) //чтение параметров 
                    {
                        int b_id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        double sum = reader.GetDouble(2);
                        double budgetAmount = reader.GetDouble(3);

                        Budgets.Add(new Budget(b_id, name, sum, budgetAmount));// наполнение списка бюджетами
                    }
                }
            }
        }
        if (Budgets.Count == 0) // если список пустой, то добавляем основной бюджет
        {
            string query2 = "INSERT INTO budgets (b_id, name, sum, budgetAmount) VALUES (@b_id, @name, @sum, @budgetAmount)";
            using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
            {
                connection.Open();
                try
                {
                    using (SqliteCommand command = new SqliteCommand(query2, connection))
                    {
                        command.Parameters.AddWithValue("@b_id", 0);
                        command.Parameters.AddWithValue("@name", "main");
                        command.Parameters.AddWithValue("@sum", .0);
                        command.Parameters.AddWithValue("@budgetAmount", .0);
                        command.ExecuteNonQuery();
                    }

                    Budgets.Add(new Budget(0, "main", 0, 0));
                }
                catch (Exception ex) // обработка ошибок
                {
                    MessageBox.Show($"{ex.Message}", "", MessageBoxButton.OK);
                }
            }
        }
    }
    //метод для добавления бюджета
    void AddBudget(string name, double sum)
    {
        string query = @"INSERT INTO budgets (name, sum, budgetAmount) VALUES (@name, @sum, @budgetAmount) RETURNING b_id";
        int b_id;
        using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
        {
            connection.Open();
            try
            {
                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@sum", sum);
                    command.Parameters.AddWithValue("@budgetAmount", sum);
                    b_id = Convert.ToInt32(command.ExecuteScalar());
                }
                var b = new Budget(b_id, name, sum, sum);
                b.AddTransaction(sum, DateTime.Now, Category.Others, "CREATE");
                Budgets.Add(b);
            }
            catch (Exception ex) { MessageBox.Show($"{ex.Message}", "", MessageBoxButton.OK); }
        }
    }

    private void bd_remove_t(Transaction t)
    {// удаление транзакции из бд
        string query = @"DELETE FROM transactions WHERE id = @id";
        using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    SqliteCommand command = new SqliteCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@id", t.id);
                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "", MessageBoxButton.OK); transaction.Rollback();
                }
            }
        }
    }
    private void bd_remove_b(Budget b)
    {
        string query = @"DELETE FROM budgets WHERE b_id = @b_id";
        using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    SqliteCommand command = new SqliteCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@b_id", b.b_id);
                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "", MessageBoxButton.OK); transaction.Rollback();
                }
            }
        }

    }
    private void budgets_lb_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        show_budgets_info(); //метод выводящий информацию о выбранном бюджете
    }

    private void transactions_lb_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {// обновление информации о выбранной транзакции

        try
        {
            if (transactions_lb.SelectedItem != null)
            {
                string name = "none";

                foreach (var b in Budgets) { if (b.b_id == ((Transaction)transactions_lb.SelectedItem).b_id) { name = b.name; } }
                transaction_info_tb.Text =
                    $"Сумма: {((Transaction)transactions_lb.SelectedItem).sum}\n" +
                    $"Бюджет: {name} \n" +
                    $"Категория: {((Transaction)transactions_lb.SelectedItem).cat.ToString()}\n" +
                    $"Дата: {((Transaction)transactions_lb.SelectedItem).date}\n" +
                    $"Описание: {((Transaction)transactions_lb.SelectedItem).info} \n";
            }
        }
        catch (Exception ex) { transaction_info_tb.Text = "==="; }
    }

    private void add_t_btn_Click(object sender, RoutedEventArgs e)
    {//добавление транзакции
        try
        {
            DateTime date = DateTime.Now; ;
            double sum = double.Parse(t_sum_tb.Text);
            Category cat = (Category)t_cat_cmbbx.SelectedItem;
            if (!(bool)t_check_date_now.IsChecked)
            {
                date = DateTime.Parse(t_date_picker.Text);
            }
            string? info = t_info_tb.Text;
            if (((Budget)b_picker_cmbbx.SelectedItem).sum + sum == 0) { MessageBox.Show("Недостаточно средств, баланс стал отрицательным", "", MessageBoxButton.OK, MessageBoxImage.Warning); }
            ((Budget)b_picker_cmbbx.SelectedItem).AddTransaction(sum, date, cat, info);
            get_main_budget(Budgets).transactions.Clear();
            get_main_budget(Budgets).GetTransactions();
            t_sum_tb.Text = "";
            b_picker_cmbbx.SelectedIndex = -1;
            t_cat_cmbbx.SelectedIndex = -1;
            t_check_date_now.IsChecked = false;
            t_date_picker.Text = "";
            t_info_tb.Text = "";
            show_budgets_info();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex}", "Некорректный ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void add_b_btn_Click(object sender, RoutedEventArgs e)
    {// добавление бюджета после нажатия на кнопку
        try
        {
            if (b_name_tb.Text != "" && amountB_txtb.Text != "" && double.Parse(amountB_txtb.Text) >= 0)
            {


                string name = b_name_tb.Text;
                double amountBudget = double.Parse(amountB_txtb.Text);

                if (get_main_budget(Budgets).sum < amountBudget)
                {
                    MessageBox.Show("Сумма не может быть больше общего баланса", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else { AddBudget(name, amountBudget); }
                b_name_tb.Text = "";
                amountB_txtb.Text = "";
            }
            else
            {
                MessageBox.Show("Некорректный ввод", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
        catch (Exception ex) { MessageBox.Show($"{ex.Message}", "", MessageBoxButton.OK); }
    }

    void show_budgets_info()
    {
        try
        {
            // вставка нужной информации о выбранном бюджете
            if (budgets_lb.SelectedItem != null)
            {
                var selectedBudget = (Budget)budgets_lb.SelectedItem;

                if (selectedBudget.name == "main")
                {
                    budget_info_tb.Text = $"Баланс: {selectedBudget.sum}";
                }
                else
                {
                    budget_info_tb.Text = $"Баланс: {selectedBudget.sum}\nВыделенный бюджет: {selectedBudget.budgetAmount}";
                }

                // Очистка списка транзакций и вставка нужного
                if (selectedBudget.transactions.Count != 0)
                {
                    // Убедитесь, что Items пуст перед присвоением ItemsSource
                    if (transactions_lb.ItemsSource == null) { transactions_lb.Items.Clear(); }
                    transactions_lb.ItemsSource = selectedBudget.transactions;
                }
                else
                {
                    // Если транзакций нет, используйте ItemsSource для отображения сообщения
                    transactions_lb.ItemsSource = null; // Сначала очищаем ItemsSource
                    transactions_lb.Items.Clear();
                    transactions_lb.Items.Add("Нет транзакций");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message}", "", MessageBoxButton.OK);
        }

    }

    private void create_report_Click(object sender, RoutedEventArgs e) { CreateReport(); }// отчет
    //создание отчета
    void CreateReport()
    {
        try
        {
            using (StreamWriter sw = new StreamWriter($"{DateTime.Now.ToString("dd/MM/yyyy")}_report.txt"))
            {
                foreach (var b in Budgets)
                {
                    sw.WriteLine($"{b.name}:    ");
                    foreach (var t in b.transactions)
                    {
                        sw.WriteLine($"{t.sum}  {t.cat.ToString()}  {t.date}    {t.info}");
                    }
                    sw.WriteLine("\n");
                }
            }
            MessageBox.Show($"Отчет создан!", "", MessageBoxButton.OK);
        }
        catch (Exception ex) { MessageBox.Show($"{ex}", "", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void del_t_btn_Click(object sender, RoutedEventArgs e)
    {//удаление транзакции
        if (transactions_lb.SelectedItem != null && transactions_lb.ItemsSource != null && budgets_lb.SelectedItem != null)
        {
            //выбрана транзакция, транзакции есть, выбранная транзакция соответствует выбранному бюджету, бюджет выбран
            try
            {
                var t = (Transaction)transactions_lb.SelectedItem;
                bd_remove_t(t);//сначала удаляем из бд
                if (budget_exists(t.b_id))
                {
                    Budgets.First(b => b.b_id == t.b_id) .GetTransactions();
                }// удаляем из бюджета если он есть и обновляем список транзакций
                get_main_budget(Budgets).GetTransactions();// обновляем транзакции в основном бюджете
                show_budgets_info();
            }
            catch (Exception ex) { MessageBox.Show($"{ex}", "", MessageBoxButton.OK, MessageBoxImage.Warning); }

        }
        else { MessageBox.Show($"Что-то пошло не так.", "", MessageBoxButton.OK); }
    }
    private void del_b_btn_Click(object sender, RoutedEventArgs e)
    {
        if (budgets_lb.ItemsSource != null && budgets_lb.SelectedItem != null)
        {// бюджеты существуют, бюджет выбран
         //удаляем сначала из бд а потом из коллекции

            var b = (Budget)budgets_lb.SelectedItem;
            if (b.name != "main")
            {
                try
                {
                    bd_remove_b(b);
                    Budgets.Remove((Budget)budgets_lb.SelectedItem);
                    transactions_lb.ItemsSource = null;
                    transactions_lb.Items.Clear();
                }
                catch (Exception ex) { MessageBox.Show($"{ex}", "", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            else { MessageBox.Show($"Нельзя удалить основной бюджет.", "", MessageBoxButton.OK); }
        }
        else { MessageBox.Show($"Что-то пошло не так.", "", MessageBoxButton.OK); }
    }

    private void info_button_Click(object sender, RoutedEventArgs e) { show_info(); }//вывод справки
    void show_info() { MessageBox.Show("Бюджет main хранит все транзакции, которые добавляет пользователь\n\"Транзакция\" - изменение баланса, \"бюджет\" - может хранить транзакции если в него добавить.\nДля того чтобы добавить расходы, нужно писать сумму с минусом: \"-100\"\nПри создании нового бюджета в нем создается транзакция, которая не учитывается в основном бюджете и содержит сумму равную выделенной на этот бюджет, ее можно удалить, если она не нужна.\n", "", MessageBoxButton.OK, MessageBoxImage.Question); }
    bool budget_exists(int b_id)
    {
        foreach (var b in Budgets) { if (b.b_id == b_id) { return true; } }
        return false;
    }//проверяет есть ли бюджет

    public Budget get_main_budget(ObservableCollection<Budget> arr)
    {
        return arr.First(b => b.name == "main");//возвращает основной бюджет
    }
}

public class Transaction
{
    //класс "транзакция", отражает изменение баланса
    public int id { get; set; } //id для бд
    public int b_id { get; set; } // для принадлежности к бюджету
    public double sum { get; set; } // сумма, которая добавилась/убавилась 
    public string? info { get; set; } // описание
    public DateTime date { get; set; } // дата
    public Category cat { get; set; } // категория трат
    public Transaction(int id, int b_id, double sum, string? info, DateTime date, Category cat)
    {
        this.id = id;
        this.b_id = b_id;
        this.sum = sum;
        this.info = info;
        this.date = date;
        this.cat = cat;
    }
}
public class Budget
{
    public int b_id { get; set; } // id для бд
    public string name { get; set; } // имя бюджета
    public double sum { get; set; } // текущее кол-во денег
    public double budgetAmount { get; set; } // максимально сколько можно потратить, думаю просто выделять с основного баланса некоторое кол-во денег и просто чтобы был виден остаток и сколько было выделено
    public ObservableCollection<Transaction> transactions; // список транзакций принадлежащих к нему

    public Budget(int b_id, string name, double sum, double budgetAmount)
    {
        this.name = name;
        this.sum = sum;
        this.budgetAmount = budgetAmount;
        this.transactions = new ObservableCollection<Transaction>();
        this.b_id = b_id;

        this.GetTransactions();
    }
    private void update_sum()
    {//обновление суммы на балансе
        this.sum = .0;
        if (this.name == "main")
        {
            foreach (var t in transactions)
            {
                if (t.info != "CREATE") { this.sum += t.sum; }

            }
        }
        else { foreach (var t in transactions) { this.sum += t.sum; } }
    }
    public void GetTransactions()
    {//чтение транзакций из бд
        transactions.Clear();
        string query;
        if (this.name == "main") { query = "SELECT id, b_id, sum, info, date, cat FROM transactions"; } // если название main то читаем все, main будет хранить всё
        else { query = "SELECT id, b_id, sum, info, date, cat FROM transactions WHERE b_id = @b_id"; }

        using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
        {
            connection.Open();
            using (SqliteCommand command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@b_id", this.b_id);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        int b_id = reader.GetInt32(1);
                        double sum = reader.GetDouble(2);
                        string? info = reader.IsDBNull(3) ? null : reader.GetString(3);
                        DateTime date = reader.GetDateTime(4);
                        Category cat = (Category)reader.GetInt32(5);

                        transactions.Add(new Transaction(id, b_id, sum, info, date, cat)); //заполнение бюджета транзакциями
                    }
                }
            }
        }
        this.update_sum();
    }
    public void AddTransaction(double sum, DateTime date, Category cat, string? info = null)
    {// добавление транзакции в список внутри объекта и в бд
        string query = "INSERT INTO transactions (b_id, sum, date, cat, info) VALUES (@b_id, @sum, @date, @cat, @info) RETURNING id";

        using (SqliteConnection connection = new SqliteConnection(CONNECTION_STRING))
        {
            connection.Open();

            try
            {
                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@b_id", this.b_id);
                    command.Parameters.AddWithValue("@sum", sum);
                    command.Parameters.AddWithValue("@date", date);
                    command.Parameters.AddWithValue("@cat", (int)cat);
                    command.Parameters.AddWithValue("@info", info);

                    int id = Convert.ToInt32(command.ExecuteScalar());

                    transactions.Add(new Transaction(id, b_id, sum, info, date, cat));
                }
                this.update_sum();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "", MessageBoxButton.OK);
            }
        }

    }
}