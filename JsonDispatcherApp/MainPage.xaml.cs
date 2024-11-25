using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Controls;
using System.IO;
using System.Collections.Generic;

namespace JsonDispatcherApp;

public partial class MainPage : ContentPage
{
    private ObservableCollection<Student> _students = new(); // Основний список студентів
    private ObservableCollection<Student> _filteredStudents = new(); // Відфільтрований список
    private List<Student> _originalStudents; // Оригінальний список студентів (для скидання фільтрів)
    private Student _selectedStudent; // Обраний студент для редагування
    private readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true }; // Опції JSON

    public MainPage()
    {
        InitializeComponent();
        StudentsGrid.ItemsSource = _filteredStudents;
    }

    // Завантаження JSON-файлу через кнопку
    private async void LoadJson_Clicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.json" } },
                { DevicePlatform.Android, new[] { "application/json" } },
                { DevicePlatform.WinUI, new[] { ".json" } },
                { DevicePlatform.MacCatalyst, new[] { "json" } }
            });

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Оберіть JSON-файл",
                FileTypes = customFileType
            });

            if (result != null)
            {
                var json = await File.ReadAllTextAsync(result.FullPath);
                var students = JsonSerializer.Deserialize<List<Student>>(json, JsonOptions);
                _students = new ObservableCollection<Student>(students ?? new List<Student>());
                _originalStudents = new List<Student>(_students); // Ініціалізація після завантаження даних
                FilterStudents();
                await DisplayAlert("Успіх", "Дані завантажено!", "ОК");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Не вдалося завантажити файл: {ex.Message}", "ОК");
        }
    }

    // Збереження даних у JSON
    private async void SaveJson_Clicked(object sender, EventArgs e)
    {
        try
        {
            var fileName = "students.json";
            var json = JsonSerializer.Serialize(_students, JsonOptions);
            var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            await File.WriteAllTextAsync(filePath, json);
            await DisplayAlert("Успіх", $"Дані збережено у файл {filePath}!", "ОК");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Не вдалося зберегти дані: {ex.Message}", "ОК");
        }
    }

    // Пошук і фільтрація даних
    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterStudents(e.NewTextValue);
    }

    private void FilterStudents(string keyword = "")
    {
        var filtered = _students.Where(s =>
            string.IsNullOrEmpty(keyword) ||
            s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            s.Faculty.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            s.Specialty.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        _filteredStudents.Clear();
        foreach (var student in filtered)
        {
            _filteredStudents.Add(student);
        }
    }

    // Форма для фільтрів
    private void Filters_Clicked(object sender, EventArgs e)
    {
        StudentsGrid.IsVisible = false;
        FilterForm.IsVisible = true;
    }

    // Застосування фільтрів
    private void ApplyFilters_Clicked(object sender, EventArgs e)
    {
        string courseFilter = CourseFilterEntry.Text;
        string minGPAFilter = GPAFilterEntry.Text;
        string facultyFilter = FacultyFilterEntry.Text;

        var filtered = _students.Where(s =>
            (string.IsNullOrEmpty(courseFilter) || s.Course.ToString() == courseFilter) &&
            (string.IsNullOrEmpty(minGPAFilter) || s.GPA >= double.Parse(minGPAFilter)) &&
            (string.IsNullOrEmpty(facultyFilter) || s.Faculty.Contains(facultyFilter, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        _filteredStudents.Clear();
        foreach (var student in filtered)
        {
            _filteredStudents.Add(student);
        }

        StudentsGrid.IsVisible = true;
        FilterForm.IsVisible = false;
    }

    // Кнопка "Назад" на сторінці фільтрів
    private void BackFromFilters_Clicked(object sender, EventArgs e)
    {
        _filteredStudents.Clear();
        foreach (var student in _originalStudents)
        {
            _filteredStudents.Add(student);
        }

        StudentsGrid.IsVisible = true;
        FilterForm.IsVisible = false;
    }

    // Підтвердження перед виходом
    private async void Exit_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Вихід", "Ви дійсно бажаєте вийти?", "Так", "Ні");
        if (confirm)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }

    // Додати студента
    private async void AddStudent_Clicked(object sender, EventArgs e)
    {
        var newStudent = new Student
        {
            ID = _students.Count + 1,
            Name = "Новий студент",
            Faculty = "Факультет",
            Specialty = "Спеціальність",
            Course = 1,
            GPA = 0.0
        };
        _students.Add(newStudent);
        FilterStudents();
        await DisplayAlert("Успіх", "Студента додано!", "ОК");
    }

    // Редагування студента в одному вікні
    private async void EditStudent_Clicked(object sender, EventArgs e)
    {
        if (StudentsGrid.SelectedItem is Student selected)
        {
            _selectedStudent = selected;
            NameEntry.Text = selected.Name;
            FacultyEntry.Text = selected.Faculty;
            SpecialtyEntry.Text = selected.Specialty;
            CourseEntry.Text = selected.Course.ToString();
            GPAEntry.Text = selected.GPA.ToString();

            StudentsGrid.IsVisible = false;
            EditForm.IsVisible = true;
        }
        else
        {
            await DisplayAlert("Помилка", "Будь ласка, виберіть студента для редагування.", "ОК");
        }
    }

    // Збереження змін студенту
    private void SaveStudent_Clicked(object sender, EventArgs e)
    {
        if (_selectedStudent != null)
        {
            _selectedStudent.Name = NameEntry.Text;
            _selectedStudent.Faculty = FacultyEntry.Text;
            _selectedStudent.Specialty = SpecialtyEntry.Text;
            _selectedStudent.Course = int.Parse(CourseEntry.Text);
            _selectedStudent.GPA = double.Parse(GPAEntry.Text);

            FilterStudents();

            StudentsGrid.IsVisible = true;
            EditForm.IsVisible = false;
        }
    }

    // Кнопка Назад
    private void BackButton_Clicked(object sender, EventArgs e)
    {
        StudentsGrid.IsVisible = true;
        EditForm.IsVisible = false;
    }

    // Видалення студента
    private async void DeleteStudent_Clicked(object sender, EventArgs e)
    {
        if (StudentsGrid.SelectedItem is Student selected)
        {
            _students.Remove(selected);
            FilterStudents();
            await DisplayAlert("Успіх", "Студента видалено!", "ОК");
        }
        else
        {
            await DisplayAlert("Помилка", "Будь ласка, виберіть студента для видалення.", "ОК");
        }
    }
}
