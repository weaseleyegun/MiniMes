namespace MiniMes.OperatorUI;

// 화면 전체에서 쓰는 색상과 글꼴
public static class Theme
{
    public const string FontName = "Malgun Gothic";

    public static readonly Color Bg = Color.FromArgb(10, 17, 30);
    public static readonly Color Panel = Color.FromArgb(18, 28, 46);
    public static readonly Color Card = Color.FromArgb(15, 24, 40);
    public static readonly Color CardBorder = Color.FromArgb(34, 51, 76);
    public static readonly Color Line = Color.FromArgb(38, 53, 74);
    public static readonly Color Text = Color.FromArgb(232, 237, 242);
    public static readonly Color Muted = Color.FromArgb(138, 153, 168);
    public static readonly Color Accent = Color.FromArgb(77, 163, 255);
    public static readonly Color Teal = Color.FromArgb(45, 212, 191);
    public static readonly Color Orange = Color.FromArgb(245, 158, 66);
    public static readonly Color Purple = Color.FromArgb(167, 139, 250);
    public static readonly Color Red = Color.FromArgb(239, 83, 80);
    public static readonly Color Green = Color.FromArgb(102, 187, 106);
    public static readonly Color Yellow = Color.FromArgb(242, 183, 5);

    public static Font Font(float size, bool bold = false) =>
        new(FontName, size, bold ? FontStyle.Bold : FontStyle.Regular);

    // 판정 문구에 따른 (배경, 글자) 색
    public static (Color Back, Color Fore) JudgeColors(string? judgement) => judgement switch
    {
        "정상" => (Color.FromArgb(31, 77, 42), Color.FromArgb(185, 240, 194)),
        "주의" or "중간 관련" => (Color.FromArgb(90, 67, 0), Color.FromArgb(255, 224, 138)),
        "초과" or "강한 관련" => (Color.FromArgb(92, 31, 31), Color.FromArgb(255, 180, 180)),
        _ => (Color.FromArgb(47, 57, 68), Color.FromArgb(184, 196, 207)),
    };

    // 설비 상태별 색
    public static Color StatusColor(string? status) => status switch
    {
        "Run" => Green,
        "Idle" => Yellow,
        "Alarm" => Red,
        _ => Muted,
    };

    public static string StatusText(string? status) => status switch
    {
        "Run" => "가동 중",
        "Idle" => "대기",
        "Alarm" => "알람",
        _ => status ?? "-",
    };
}
