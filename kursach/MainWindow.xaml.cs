//sqlite как бд
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Windows;
namespace kursach;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    const string CONNECTION_STRING = "data source=user_data.db";


    public ObservableCollection<Budget> Budgets { get; set; } = new ObservableCollection<Budget>();


    public enum Category
    {
        // убрать лишние =
        Food = 0,          // Продукты питания и напитки
        Housing = 1,       // Жилье (аренда, ипотека, коммунальные услуги)
        Utilities = 2,     // Коммунальные услуги (электричество, вода, газ, интернет, телефон)
        Transportation = 3,// Транспорт (бензин, общественный транспорт, ремонт авто)
        Health = 4,        // Здоровье (медицинские услуги, лекарства, страхование)
        Education = 5,     // Образование (книги, курсы, обучение)
        Clothing = 6,      // Одежда и обувь
        Entertainment = 7, // Развлечения (кино, концерты, хобби)
        PersonalCare = 8,  // Уход за собой (косметика, парикмахерские услуги)
        Gifts = 9,         // Подарки и поздравления
        Insurance = 10,    // Страхование (жизни, имущества)
        Investments = 11,  // Инвестиции
        Savings = 12,      // Накопления
        Others = 13 // Прочие расходы
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
        b_id INT PRIMARY KEY AUTOINCREMENT, 
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
                    MessageBox.Show( $"что-то пошло не так...\n{ex.Message}", "", MessageBoxButton.OK);
                }
            }
        }
    }
    //aaa
    public class Transaction
    {
        //класс "транзакция", отражает изменение баланса
        int id { get; set; } //id для бд
        int b_id { get; set; } // для принадлежности к бюджету
        public double sum { get; set; } // сумма, которая добавилась/убавилась 
        string? info { get; set; } // описание
        DateTime date { get; set; } // дата
        Category cat { get; set; } // категория трат
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
        int b_id { get; set; } // id для бд
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
            foreach (var tr in this.transactions)
            {
                this.sum += tr.sum;
            }
        }
        private void GetTransactions()
        {//чтение транзакций из бд
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
                SqliteTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqliteCommand command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@b_id", this.b_id);
                        command.Parameters.AddWithValue("@sum", sum);
                        command.Parameters.AddWithValue("@date", date);
                        command.Parameters.AddWithValue("@cat", (int)cat);
                        command.Parameters.AddWithValue("@info", info);

                        int id = (int)command.ExecuteScalar();

                        transactions.Add(new Transaction(id, b_id, sum, info, date, cat));
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("", $"что-то пошло не так...\n{ex.Message}", MessageBoxButton.OK);
                }
            }
            this.update_sum();
        }
    }

    private void budgets_lb_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {//очишение списка транзакций и вставка вместо него нужного
        transactions_lb.Items.Clear();
        if (((Budget)budgets_lb.SelectedItem).transactions.Count() > 0)
        {
            transactions_lb.ItemsSource = ((Budget)budgets_lb.SelectedItem).transactions;
        }
        else {
            transactions_lb.Items.Add("нет транзакций");
        }
    }
}