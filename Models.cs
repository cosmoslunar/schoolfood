namespace MealNotifier;

public class MealEntry
{
    public string Date { get; set; } = "";
    public string Menu { get; set; } = "";
    public string Time { get; set; } = "12:30";
    public string Gender { get; set; } = "공통";
}

public class MealSettings
{
    public string CommonTime { get; set; } = "12:30";
    public Dictionary<string, string> WeeklyTimes { get; set; } = new();
    public Dictionary<string, string> DailyTimes { get; set; } = new();

    public string CommonGender { get; set; } = "공통";
    public Dictionary<string, string> WeeklyGenders { get; set; } = new();
    public Dictionary<string, string> DailyGenders { get; set; } = new();
}

public class MealData
{
    public List<MealEntry> Meals { get; set; } = new();
    public MealSettings Settings { get; set; } = new();
}
