using System.Text.Json;
using System.Text.Encodings.Web;

namespace MealNotifier;

public static class DataStore
{
    public static readonly string DataDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MealNotifier");

    public static readonly string DataFile = Path.Combine(DataDirectory, "meals.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static MealData Load()
    {
        Directory.CreateDirectory(DataDirectory);
        if (!File.Exists(DataFile))
        {
            var data = new MealData();
            Save(data);
            return data;
        }

        try
        {
            var json = File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<MealData>(json, Options) ?? new MealData();
        }
        catch
        {
            return new MealData();
        }
    }

    public static void Save(MealData data)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(DataFile, JsonSerializer.Serialize(data, Options));
    }
}
