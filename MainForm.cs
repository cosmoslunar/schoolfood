using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace MealNotifier;

public class MainForm : Form
{
    private MealData data = DataStore.Load();
    private readonly DataGridView grid = new();
    private readonly Label dateLabel = new();
    private readonly Label mealLabel = new();
    private readonly Label timeLabel = new();
    private readonly Label countdownLabel = new();
    private readonly Label genderLabel = new();
    private readonly NotifyIcon tray = new();
    private readonly System.Windows.Forms.Timer timer = new();
    private DateTime? notifiedThreeMinute;
    private DateTime? notifiedMealTime;
    private bool adminMode = false;

    public MainForm()
    {
        Text = "급식 알리미";
        Width = 1100;
        Height = 700;
        MinimumSize = new Size(850, 550);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(15, 20, 28);
        ForeColor = Color.White;
        KeyPreview = true;

        BuildMainUi();
        BuildTray();

        timer.Interval = 1000;
        timer.Tick += (_, _) => RefreshDisplay();
        timer.Start();

        KeyDown += MainForm_KeyDown;
        FormClosing += (_, e) =>
        {
            if (!adminMode)
            {
                e.Cancel = true;
                Hide();
            }
        };

        RefreshDisplay();
    }

    void BuildMainUi()
    {
        var title = new Label
        {
            Text = "🍚 오늘의 급식",
            Dock = DockStyle.Top,
            Height = 80,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("맑은 고딕", 28, FontStyle.Bold)
        };
        Controls.Add(title);

        dateLabel.Dock = DockStyle.Top;
        dateLabel.Height = 45;
        dateLabel.TextAlign = ContentAlignment.MiddleCenter;
        dateLabel.Font = new Font("맑은 고딕", 16);
        Controls.Add(dateLabel);

        var center = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(70, 20, 70, 40)
        };
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        mealLabel.Dock = DockStyle.Fill;
        mealLabel.TextAlign = ContentAlignment.MiddleCenter;
        mealLabel.Font = new Font("맑은 고딕", 22, FontStyle.Bold);
        mealLabel.BackColor = Color.FromArgb(28, 36, 48);

        countdownLabel.Dock = DockStyle.Fill;
        countdownLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownLabel.Font = new Font("맑은 고딕", 25, FontStyle.Bold);

        timeLabel.Dock = DockStyle.Fill;
        timeLabel.TextAlign = ContentAlignment.MiddleCenter;
        timeLabel.Font = new Font("맑은 고딕", 16);

        genderLabel.Dock = DockStyle.Fill;
        genderLabel.TextAlign = ContentAlignment.MiddleCenter;
        genderLabel.Font = new Font("맑은 고딕", 14);

        center.Controls.Add(mealLabel, 0, 0);
        center.Controls.Add(countdownLabel, 0, 1);
        center.Controls.Add(timeLabel, 0, 2);
        center.Controls.Add(genderLabel, 0, 3);
        Controls.Add(center);

        var help = new Label
        {
            Text = "관리자: Ctrl + Shift + A    |    종료: 관리자 화면에서 종료",
            Dock = DockStyle.Bottom,
            Height = 35,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray
        };
        Controls.Add(help);
    }

    void BuildTray()
    {
        tray.Icon = SystemIcons.Information;
        tray.Text = "급식 알리미";
        tray.Visible = true;
        tray.DoubleClick += (_, _) => ShowMain();
        var menu = new ContextMenuStrip();
        menu.Items.Add("화면 열기", null, (_, _) => ShowMain());
        menu.Items.Add("관리자 화면", null, (_, _) => OpenAdmin());
        menu.Items.Add("종료", null, (_, _) => ExitApplication());
        tray.ContextMenuStrip = menu;
    }

    void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.Shift && e.KeyCode == Keys.A)
        {
            e.SuppressKeyPress = true;
            OpenAdmin();
        }
    }

    void RefreshDisplay()
    {
        var now = DateTime.Now;
        dateLabel.Text = now.ToString("yyyy년 M월 d일 dddd", CultureInfo.GetCultureInfo("ko-KR"));

        var entry = data.Meals.FirstOrDefault(x => x.Date == now.ToString("yyyy-MM-dd"));
        if (entry == null)
        {
            mealLabel.Text = "오늘 등록된 급식이 없습니다.";
            countdownLabel.Text = "";
            timeLabel.Text = "";
            genderLabel.Text = "";
            return;
        }

        var effectiveTime = ResolveTime(now.Date, entry);
        var effectiveGender = ResolveGender(now.Date, entry);

        mealLabel.Text = FormatMenu(entry.Menu);
        genderLabel.Text = $"급식 구분: {effectiveGender}";

        if (!TimeSpan.TryParse(effectiveTime, out var ts))
        {
            timeLabel.Text = "급식시간 설정 오류";
            countdownLabel.Text = "";
            return;
        }

        var mealAt = now.Date.Add(ts);
        timeLabel.Text = $"급식시간  {effectiveTime}";

        if (now < mealAt)
        {
            var remain = mealAt - now;
            countdownLabel.Text = $"급식까지  {remain.Hours:00}:{remain.Minutes:00}:{remain.Seconds:00}";
            HandleNotifications(now, mealAt);
        }
        else
        {
            countdownLabel.Text = "현재 급식시간이 지났습니다.";
            if (now - mealAt < TimeSpan.FromMinutes(1))
                HandleMealNotification(now, mealAt);
        }
    }

    static string FormatMenu(string menu)
    {
        var items = menu.Split(new[] { '\n', ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .ToArray();
        return items.Length == 0 ? "메뉴 없음" : string.Join("\n", items.Select(x => "• " + x));
    }

    string ResolveTime(DateTime date, MealEntry entry)
    {
        var key = date.ToString("yyyy-MM-dd");
        if (data.Settings.DailyTimes.TryGetValue(key, out var d)) return d;

        var day = DayKey(date.DayOfWeek);
        if (data.Settings.WeeklyTimes.TryGetValue(day, out var w)) return w;

        return string.IsNullOrWhiteSpace(entry.Time) ? data.Settings.CommonTime : entry.Time;
    }

    string ResolveGender(DateTime date, MealEntry entry)
    {
        var key = date.ToString("yyyy-MM-dd");
        if (data.Settings.DailyGenders.TryGetValue(key, out var d)) return d;

        var day = DayKey(date.DayOfWeek);
        if (data.Settings.WeeklyGenders.TryGetValue(day, out var w)) return w;

        return string.IsNullOrWhiteSpace(entry.Gender) ? data.Settings.CommonGender : entry.Gender;
    }

    static string DayKey(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "월",
        DayOfWeek.Tuesday => "화",
        DayOfWeek.Wednesday => "수",
        DayOfWeek.Thursday => "목",
        DayOfWeek.Friday => "금",
        DayOfWeek.Saturday => "토",
        _ => "일"
    };

    void HandleNotifications(DateTime now, DateTime mealAt)
    {
        if (mealAt - now <= TimeSpan.FromMinutes(3) &&
            mealAt - now > TimeSpan.Zero &&
            notifiedThreeMinute?.Date != now.Date)
        {
            notifiedThreeMinute = now.Date;
            tray.ShowBalloonTip(7000, "🍚 급식 알림", "3분 후 급식시간입니다.", ToolTipIcon.Info);
        }

        if (now >= mealAt && notifiedMealTime?.Date != now.Date)
            HandleMealNotification(now, mealAt);
    }

    void HandleMealNotification(DateTime now, DateTime mealAt)
    {
        if (notifiedMealTime?.Date == now.Date) return;
        notifiedMealTime = now.Date;
        tray.ShowBalloonTip(7000, "🍚 급식 알림", "급식시간입니다.", ToolTipIcon.Info);
    }

    void OpenAdmin()
    {
        adminMode = true;
        using var form = new AdminForm(data);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            data = form.Result;
            DataStore.Save(data);
            RefreshDisplay();
        }
        adminMode = false;
    }

    void ShowMain()
    {
        Show();
        WindowState = FormWindowState.Maximized;
        Activate();
    }

    void ExitApplication()
    {
        tray.Visible = false;
        Application.Exit();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        BeginInvoke(() => Hide());
    }
}

public class AdminForm : Form
{
    public MealData Result { get; private set; }
    private readonly DataGridView grid = new();
    private readonly MealData working;

    public AdminForm(MealData source)
    {
        working = new MealData
        {
            Meals = source.Meals.Select(x => new MealEntry { Date=x.Date, Menu=x.Menu, Time=x.Time, Gender=x.Gender }).ToList(),
            Settings = new MealSettings
            {
                CommonTime = source.Settings.CommonTime,
                CommonGender = source.Settings.CommonGender,
                WeeklyTimes = new(source.Settings.WeeklyTimes),
                DailyTimes = new(source.Settings.DailyTimes),
                WeeklyGenders = new(source.Settings.WeeklyGenders),
                DailyGenders = new(source.Settings.DailyGenders)
            }
        };
        Result = working;

        Text = "급식 알리미 - 관리자";
        Width = 1150;
        Height = 750;
        StartPosition = FormStartPosition.CenterParent;

        Build();
    }

    void Build()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };

        var mealTab = new TabPage("급식 입력");
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = true;
        grid.AllowUserToDeleteRows = true;
        grid.RowHeadersVisible = false;
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText="날짜 (YYYY-MM-DD)", DataPropertyName="Date", Width=150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText="메뉴 (쉼표 또는 줄바꿈)", DataPropertyName="Menu", Width=550 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText="시간", DataPropertyName="Time", Width=100 });
        grid.Columns.Add(new DataGridViewComboBoxColumn { HeaderText="구분", DataPropertyName="Gender", Width=100,
            DataSource = new[] { "공통", "남", "여" }});
        grid.DataSource = new BindingSource { DataSource = working.Meals };

        var mealButtons = new FlowLayoutPanel { Dock=DockStyle.Bottom, Height=50 };
        AddButton(mealButtons, "행 추가", () => grid.Rows.Add());
        AddButton(mealButtons, "선택 행 삭제", () => { if (!grid.CurrentRow!.IsNewRow) grid.Rows.Remove(grid.CurrentRow); });
        AddButton(mealButtons, "오늘 데이터 추가", () =>
        {
            var date = DateTime.Today.ToString("yyyy-MM-dd");
            working.Meals.Add(new MealEntry { Date=date });
            grid.DataSource = new BindingSource { DataSource=working.Meals };
        });
        mealTab.Controls.Add(grid);
        mealTab.Controls.Add(mealButtons);

        var settingTab = new TabPage("시간 / 남녀 설정");
        settingTab.Controls.Add(BuildSettingsPanel());

        tabs.TabPages.Add(mealTab);
        tabs.TabPages.Add(settingTab);
        Controls.Add(tabs);

        var bottom = new FlowLayoutPanel { Dock=DockStyle.Bottom, Height=55 };
        AddButton(bottom, "자동 실행 등록", RegisterStartup);
        AddButton(bottom, "JSON 폴더 열기", () => Process.Start("explorer.exe", DataStore.DataDirectory));
        AddButton(bottom, "저장", SaveAndClose);
        AddButton(bottom, "취소", () => { DialogResult=DialogResult.Cancel; Close(); });
        Controls.Add(bottom);
    }

    Control BuildSettingsPanel()
    {
        var p = new Panel { Dock=DockStyle.Fill, Padding=new Padding(20) };
        var y = 20;

        p.Controls.Add(MakeLabel("공통 급식 시간", 20, y));
        var commonTime = new TextBox { Left=180, Top=y, Width=100, Text=working.Settings.CommonTime };
        p.Controls.Add(commonTime); y += 45;

        p.Controls.Add(MakeLabel("공통 구분", 20, y));
        var commonGender = new ComboBox { Left=180, Top=y, Width=100, DropDownStyle=ComboBoxStyle.DropDownList };
        commonGender.Items.AddRange(new[] {"공통","남","여"});
        commonGender.Text = working.Settings.CommonGender;
        p.Controls.Add(commonGender); y += 60;

        p.Controls.Add(MakeLabel("요일별 시간 (예: 월=12:30)", 20, y));
        var weeklyTime = new TextBox { Left=230, Top=y, Width=500, Text=FormatDict(working.Settings.WeeklyTimes) };
        p.Controls.Add(weeklyTime); y += 45;

        p.Controls.Add(MakeLabel("날짜별 시간 (예: 2026-10-02=12:20)", 20, y));
        var dailyTime = new TextBox { Left=230, Top=y, Width=500, Text=FormatDict(working.Settings.DailyTimes) };
        p.Controls.Add(dailyTime); y += 60;

        p.Controls.Add(MakeLabel("요일별 구분 (예: 월=남)", 20, y));
        var weeklyGender = new TextBox { Left=230, Top=y, Width=500, Text=FormatDict(working.Settings.WeeklyGenders) };
        p.Controls.Add(weeklyGender); y += 45;

        p.Controls.Add(MakeLabel("날짜별 구분 (예: 2026-10-02=여)", 20, y));
        var dailyGender = new TextBox { Left=230, Top=y, Width=500, Text=FormatDict(working.Settings.DailyGenders) };
        p.Controls.Add(dailyGender);

        p.Tag = new Control[] { commonTime, commonGender, weeklyTime, dailyTime, weeklyGender, dailyGender };
        return p;
    }

    static string FormatDict(Dictionary<string,string> d) =>
        string.Join(", ", d.Select(x => $"{x.Key}={x.Value}"));

    static Label MakeLabel(string text, int x, int y) =>
        new() { Text=text, Left=x, Top=y+3, Width=205 };

    void SaveAndClose()
    {
        if (!string.IsNullOrWhiteSpace(grid.CurrentCell?.Value?.ToString()))
            grid.EndEdit();

        var tab = Controls.OfType<TabControl>().First();
        var panel = tab.TabPages[1].Controls[0];
        var c = (Control[])panel.Tag!;

        working.Settings.CommonTime = c[0].Text.Trim();
        working.Settings.CommonGender = c[1].Text.Trim();
        working.Settings.WeeklyTimes = ParseDict(c[2].Text);
        working.Settings.DailyTimes = ParseDict(c[3].Text);
        working.Settings.WeeklyGenders = ParseDict(c[4].Text);
        working.Settings.DailyGenders = ParseDict(c[5].Text);

        working.Meals = working.Meals.Where(x => !string.IsNullOrWhiteSpace(x.Date)).ToList();

        DataStore.Save(working);
        Result = working;
        DialogResult = DialogResult.OK;
        Close();
    }

    static Dictionary<string,string> ParseDict(string text)
    {
        var d = new Dictionary<string,string>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var a = part.Split('=', 2);
            if (a.Length == 2) d[a[0].Trim()] = a[1].Trim();
        }
        return d;
    }

    static void AddButton(Control parent, string text, Action action)
    {
        var b = new Button { Text=text, AutoSize=true, Height=35, Margin=new Padding(5) };
        b.Click += (_, _) => action();
        parent.Controls.Add(b);
    }

    void RegisterStartup()
    {
        try
        {
            var exe = Application.ExecutablePath;
            var taskName = "MealNotifierAutoStart";
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Create /TN \"{taskName}\" /TR \"\\\"{exe}\\\"\" /SC ONLOGON /RL LIMITED /F",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit();
            MessageBox.Show("Windows 로그인 시 자동 실행이 등록되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("자동 실행 등록에 실패했습니다.\n" + ex.Message, "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
