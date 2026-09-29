using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Hotel.App;

public partial class Form1 : Form
{
    private const string ConnStr =
        @"Server=localhost\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private int _userId;
    private string _role = "";

    private readonly TextBox _login = new() { PlaceholderText = "Логин", Width = 130 };
    private readonly TextBox _password = new() { PlaceholderText = "Пароль", Width = 130, UseSystemPasswordChar = true };
    private readonly Button _btnLogin = new() { Text = "Войти", Width = 80 };
    private readonly Label _lblRole = new() { AutoSize = true, Text = "Вход не выполнен", Padding = new Padding(0, 6, 0, 0) };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false };

    private readonly FlowLayoutPanel _adminPanel = new() { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8), Visible = false };
    private readonly TextBox _number = new() { PlaceholderText = "Номер", Width = 70 };
    private readonly ComboBox _category = new() { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnAdd = new() { Text = "Добавить номер", Width = 120 };
    private readonly TextBox _targetLogin = new() { PlaceholderText = "Логин пользователя", Width = 130 };
    private readonly TextBox _newPass = new() { PlaceholderText = "Новый пароль", Width = 120, UseSystemPasswordChar = true };
    private readonly Button _btnChangePass = new() { Text = "Сменить пароль", Width = 120 };

    public Form1()
    {
        InitializeComponent();
        Text = "Автоматизация администрирования отеля";
        Width = 900;
        Height = 550;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(8) };
        top.Controls.AddRange(new Control[] { _login, _password, _btnLogin, _lblRole });

        _adminPanel.Controls.AddRange(new Control[]
        {
            _number, _category, _btnAdd, _targetLogin, _newPass, _btnChangePass
        });

        Controls.Add(_grid);
        Controls.Add(_adminPanel);
        Controls.Add(top);

        _btnLogin.Click += (s, e) => Run(DoLogin);
        _btnAdd.Click += (s, e) => Run(AddRoom);
        _btnChangePass.Click += (s, e) => Run(ChangePassword);
    }

    private static void Run(Action action)
    {
        try { action(); }
        catch (SqlException ex) { MessageBox.Show("Ошибка базы данных: " + ex.Message); }
    }

    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    private void DoLogin()
    {
        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        using var cmd = new SqlCommand(
            @"SELECT u.UserID, r.Name
              FROM USERS u JOIN ROLES r ON r.RoleID = u.RoleID
              WHERE u.Login = @login AND u.PasswordHash = @hash", conn);
        cmd.Parameters.AddWithValue("@login", _login.Text);
        cmd.Parameters.AddWithValue("@hash", Hash(_password.Text));

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            reader.Close();
            WriteLog(null, "Неудачный вход", "Логин: " + _login.Text);
            MessageBox.Show("Неверный логин или пароль");
            return;
        }

        _userId = reader.GetInt32(0);
        _role = reader.GetString(1);
        reader.Close();

        _lblRole.Text = $"Вы вошли как: {_role}";
        _adminPanel.Visible = _role == "admin";

        WriteLog(_userId, "Вход в систему", null);
        LoadCategories();
        LoadRooms();
    }

    private void LoadRooms()
    {
        using var conn = new SqlConnection(ConnStr);
        using var adapter = new SqlDataAdapter(
            @"SELECT r.RoomNumber AS [Номер],
                     c.Name AS [Категория],
                     c.PricePerNight AS [Цена за ночь],
                     CASE WHEN EXISTS (
                         SELECT 1 FROM BOOKINGS b
                         JOIN BOOKING_STATUSES s ON s.StatusID = b.StatusID
                         WHERE b.RoomID = r.RoomID
                           AND s.Name IN (N'Новая', N'Заселён')
                           AND CAST(GETDATE() AS DATE) >= b.CheckInDate
                           AND CAST(GETDATE() AS DATE) < b.CheckOutDate)
                          THEN N'Занят' ELSE N'Свободен' END AS [Статус]
              FROM ROOMS r
              JOIN ROOM_CATEGORIES c ON c.CategoryID = r.CategoryID
              ORDER BY r.RoomNumber", conn);
        var table = new DataTable();
        adapter.Fill(table);
        _grid.DataSource = table;
    }

    private void LoadCategories()
    {
        using var conn = new SqlConnection(ConnStr);
        using var adapter = new SqlDataAdapter("SELECT CategoryID, Name FROM ROOM_CATEGORIES", conn);
        var table = new DataTable();
        adapter.Fill(table);
        _category.DataSource = table;
        _category.DisplayMember = "Name";
        _category.ValueMember = "CategoryID";
    }

    private void AddRoom()
    {
        if (_role != "admin") return;

        if (string.IsNullOrWhiteSpace(_number.Text) || _category.SelectedValue == null)
        {
            MessageBox.Show("Введите номер и выберите категорию");
            return;
        }

        using var conn = new SqlConnection(ConnStr);
        conn.Open();
        using var cmd = new SqlCommand(
            "INSERT INTO ROOMS (RoomNumber, CategoryID) VALUES (@number, @category)", conn);
        cmd.Parameters.AddWithValue("@number", _number.Text.Trim());
        cmd.Parameters.AddWithValue("@category", (int)_category.SelectedValue!);
        cmd.ExecuteNonQuery();

        WriteLog(_userId, "Добавлен номер", "Номер: " + _number.Text.Trim());
        _number.Clear();
        LoadRooms();
    }

    private void ChangePassword()
    {
        if (_role != "admin") return;

        if (string.IsNullOrWhiteSpace(_targetLogin.Text) || string.IsNullOrWhiteSpace(_newPass.Text))
        {
            MessageBox.Show("Введите логин и новый пароль");
            return;
        }

        using var conn = new SqlConnection(ConnStr);
        conn.Open();
        using var tx = conn.BeginTransaction();

        using var find = new SqlCommand("SELECT UserID FROM USERS WHERE Login = @login", conn, tx);
        find.Parameters.AddWithValue("@login", _targetLogin.Text);
        var target = find.ExecuteScalar();
        if (target == null)
        {
            MessageBox.Show("Пользователь не найден");
            return;
        }

        using var update = new SqlCommand("UPDATE USERS SET PasswordHash = @hash WHERE UserID = @id", conn, tx);
        update.Parameters.AddWithValue("@hash", Hash(_newPass.Text));
        update.Parameters.AddWithValue("@id", (int)target);
        update.ExecuteNonQuery();

        using var log = new SqlCommand(
            "INSERT INTO PASSWORD_CHANGE_LOG (UserID, ChangedByUserID) VALUES (@target, @admin)", conn, tx);
        log.Parameters.AddWithValue("@target", (int)target);
        log.Parameters.AddWithValue("@admin", _userId);
        log.ExecuteNonQuery();

        tx.Commit();

        _targetLogin.Clear();
        _newPass.Clear();
        MessageBox.Show("Пароль изменён");
    }

    private static void WriteLog(int? userId, string action, string? description)
    {
        using var conn = new SqlConnection(ConnStr);
        conn.Open();
        using var cmd = new SqlCommand(
            "INSERT INTO ERROR_LOG (UserID, Action, Description) VALUES (@user, @action, @desc)", conn);
        cmd.Parameters.AddWithValue("@user", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@action", action);
        cmd.Parameters.AddWithValue("@desc", (object?)description ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}