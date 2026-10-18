using MiniMes.Api.Models;

namespace MiniMes.Api.Data;

public static class DbSeeder
{
    public static void SeedItems(MiniMesDbContext db)
    {
        if (db.Items.Any()) return;

        // (코드, 이름, 재질, 직경, 절삭길이, 날수)
        var rows = new (string Code, string Name, string Material, double D, double L, int F)[]
        {
            ("EM-S-06",  "엔드밀 Steel 6",      "Steel",      6, 15, 4),
            ("EM-S-10",  "엔드밀 Steel 10",     "Steel",     10, 22, 4),
            ("EM-S-12",  "엔드밀 Steel 12",     "Steel",     12, 26, 4),
            ("EM-S-16",  "엔드밀 Steel 16",     "Steel",     16, 32, 4),
            ("EM-SS-08", "엔드밀 Stainless 8",  "Stainless",  8, 19, 3),
            ("EM-SS-12", "엔드밀 Stainless 12", "Stainless", 12, 26, 3),
            ("EM-AL-10", "엔드밀 Aluminum 10",  "Aluminum",  10, 25, 2),
            ("EM-AL-16", "엔드밀 Aluminum 16",  "Aluminum",  16, 38, 2),
            ("EM-AL-20", "엔드밀 Aluminum 20",  "Aluminum",  20, 45, 3),
            ("EM-CB-06", "엔드밀 Carbide 6",    "Carbide",    6, 13, 4),
            ("EM-CB-10", "엔드밀 Carbide 10",   "Carbide",   10, 22, 6),
            ("EM-CB-12", "엔드밀 Carbide 12",   "Carbide",   12, 26, 6),
            // 신규 품목(아직 생산 이력 없음): Step 10 예측 대상
            ("EM-S-14",  "엔드밀 Steel 14",     "Steel",     14, 28, 4),
            ("EM-AL-12", "엔드밀 Aluminum 12",  "Aluminum",  12, 30, 2),
        };

        foreach (var r in rows)
        {
            db.Items.Add(new Item
            {
                ItemCode = r.Code,
                Name = r.Name,
                Material = r.Material,
                Diameter = r.D,
                CutLength = r.L,
                FluteCount = r.F
            });
        }
        db.SaveChanges();
    }
}
