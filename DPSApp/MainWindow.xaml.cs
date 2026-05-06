using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace DPSApp
{
    public partial class MainWindow : Window
    {
        // Строка подключения к вашей БД
        private string connectionString = @"Data Source=F107-SQL\IS2314;Initial Catalog=СалонСотовойСвязи;Integrated Security=True";
        private string currentTable = "";
        private SqlDataAdapter adapter;
        private DataTable dt;

        public MainWindow()
        {
            InitializeComponent();
            // Подписываемся на событие генерации колонок для их красивого оформления
            MainDataGrid.AutoGeneratingColumn += MainDataGrid_AutoGeneratingColumn;
        }

        // Обработка выбора таблицы в боковом меню
        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag != null)
            {
                currentTable = selectedItem.Tag.ToString();
                TxtTableName.Text = selectedItem.Content.ToString();
                LoadData(currentTable);
            }
        }

        // Загрузка данных из БД
        private void LoadData(string tableName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "";

                    // Настраиваем SQL-запросы для каждой таблицы
                    switch (tableName)
                    {
                        case "SIMКарты":
                            query = @"SELECT s.*, k.ФИО as Клиент_ФИО 
                                      FROM SIMКарты s 
                                      LEFT JOIN Клиенты k ON s.КлиентID = k.КлиентID";
                            break;

                        case "МоделиТелефонов":
                            query = @"SELECT m.*, b.Наименование as Бренд_Название 
                                      FROM МоделиТелефонов m 
                                      LEFT JOIN Бренды b ON m.БрендID = b.БрендID";
                            break;

                        case "Подписки":
                            query = @"SELECT p.*, s.Номер as SIM_Номер, t.НаименованиеТарифа 
                                      FROM Подписки p 
                                      LEFT JOIN SIMКарты s ON p.SIMID = s.SIMID
                                      LEFT JOIN Тарифы t ON p.ТарифID = t.ТарифID";
                            break;

                        case "ПродажиТелефонов":
                            query = @"SELECT p.*, k.ФИО as Клиент_ФИО, m.Модель as Модель_Телефона, s.ФИО as Сотрудник_ФИО 
                                      FROM ПродажиТелефонов p 
                                      LEFT JOIN Клиенты k ON p.КлиентID = k.КлиентID
                                      LEFT JOIN МоделиТелефонов m ON p.МодельID = m.МодельID
                                      LEFT JOIN Сотрудники s ON p.СотрудникID = s.СотрудникID";
                            break;

                        default:
                            // Для остальных таблиц (Бренды, Клиенты и т.д.) просто выбираем всё
                            query = $"SELECT * FROM [{tableName}]";
                            break;
                    }

                    adapter = new SqlDataAdapter(query, connection);
                    // Автоматически создаем команды Insert, Update, Delete
                    new SqlCommandBuilder(adapter);

                    dt = new DataTable();
                    adapter.Fill(dt);

                    MainDataGrid.ItemsSource = null;
                    MainDataGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "БД Салон связи");
            }
        }

        // Настройка внешнего вида колонок (Заголовки и формат дат)
        private void MainDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string header = e.PropertyName;

            switch (header)
            {
                case "КлиентID":
                case "SIMID":
                case "ТарифID":
                case "БрендID":
                case "МодельID":
                case "СотрудникID":
                case "ПродажаID":
                    e.Column.Header = "ID"; break;

                case "ФИО": e.Column.Header = "Полное имя"; break;
                case "Наименование": e.Column.Header = "Название"; break;
                case "АбонентскаяПлата": e.Column.Header = "Абон. плата"; break;
                case "Номер": e.Column.Header = "Моб. номер"; break;
                case "Активна": e.Column.Header = "Статус"; break;
                case "Клиент_ФИО": e.Column.Header = "Владелец"; break;
                case "Бренд_Название": e.Column.Header = "Производитель"; break;
                case "Модель_Телефона": e.Column.Header = "Телефон"; break;
                case "Стоимость": e.Column.Header = "Сумма (руб)"; break;
            }

            // Форматирование дат
            if (e.PropertyType == typeof(DateTime))
            {
                var column = e.Column as DataGridTextColumn;
                if (column != null)
                {
                    if (header == "ДатаПродажи")
                        ((System.Windows.Data.Binding)column.Binding).StringFormat = "dd.MM.yyyy HH:mm";
                    else
                        ((System.Windows.Data.Binding)column.Binding).StringFormat = "dd.MM.yyyy";
                }
            }

            // Скрываем лишние ID (внешние ключи), оставляя только основной ID таблицы
            string primaryKeySuffix = "ID";
            if (header.EndsWith(primaryKeySuffix) && header != currentTable.TrimEnd('ы').TrimEnd('и') + "ID")
            {
                if (header != "КлиентID" || currentTable != "Клиенты") // Исключение для таблицы Клиенты
                    e.Column.Visibility = Visibility.Collapsed;
            }
        }

        // Кнопка сохранения изменений (Исправлена ошибка ConnectionString)
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (adapter != null && dt != null)
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // Важно: перепривязываем соединение перед Update
                        adapter.SelectCommand.Connection = connection;
                        SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

                        adapter.Update(dt);
                        MessageBox.Show("Изменения успешно сохранены!", "Успех");
                        LoadData(currentTable);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message +
                    "\n\nПримечание: Изменение данных в таблицах с объединением (JOIN) может быть ограничено.");
            }
        }

        // Кнопка добавления пустой строки
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (dt != null)
            {
                DataRow newRow = dt.NewRow();
                dt.Rows.Add(newRow);
            }
            else
            {
                MessageBox.Show("Сначала выберите таблицу в меню слева.");
            }
        }

        // Кнопка удаления строки
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataGrid.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    row.Delete();
                    BtnRefresh_Click(sender, e); // Сразу сохраняем удаление
                }
            }
            else
            {
                MessageBox.Show("Выберите строку для удаления.");
            }
        }
    }
}
