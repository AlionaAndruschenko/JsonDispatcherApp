using System.Text.Json;

namespace JsonDispatcherApp;

public static class JsonHelper
{
    public static List<Student> Deserialize(string json)
    {
        return JsonSerializer.Deserialize<List<Student>>(json);
    }

    public static string Serialize(List<Student> students)
    {
        return JsonSerializer.Serialize(students, new JsonSerializerOptions { WriteIndented = true });
    }
}

