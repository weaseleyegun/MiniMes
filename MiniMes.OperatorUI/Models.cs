namespace MiniMes.OperatorUI;

// API가 돌려주는 JSON을 담는 레코드들. 속성 이름은 API 응답 필드와 같다(대소문자 무시).

// ---------- 품목 / 사이클 / 설비 ----------

public record ItemRow(
    string ItemCode,
    string Name,
    string Material,
    double Diameter,
    double CutLength,
    int FluteCount,
    double? StandardCycleTimeSec);

public record CycleRow(
    long Id,
    string ItemCode,
    string EquipmentCode,
    DateTime StartedAt,
    double CycleTimeSec,
    bool IsGood,
    double SpindleLoadAvg,
    double SpindleLoadMax,
    double SpindleRpmAvg,
    double FeedRateAvg,
    double VibrationMax);

public record EquipmentLogRow(
    string EquipmentCode,
    string Status,
    double SpindleRpm,
    double FeedRate,
    double SpindleLoadPct,
    DateTime CollectedAt);

public record EquipmentBoardRow(
    string EquipmentCode,
    string Status,
    double SpindleRpm,
    double FeedRate,
    double SpindleLoadPct,
    DateTime? LastCollectedAt,
    int RecentCycleCount,
    int RecentDefectCount,
    double DefectRatePct,
    string? LastItemCode,
    double? LastCycleTimeSec,
    double? StandardCt,
    double? DeviationPct,
    string Judgement);

// ---------- 표준화 / ACT 분석 ----------

public record StandardResult(
    string ItemCode,
    int SampleCount,
    int OutlierCount,
    double StandardCt,
    double StdDev,
    double? PreviousStandardCt);

public record RecalculateResponse(
    List<StandardResult> Updated,
    List<string> SkippedItemCodes);

public record ActVariableResult(
    string Variable,
    double CohensD,
    double DefectOutOfLimitPct,
    double GoodOutOfLimitPct,
    string Judgement);

public record ActAnalysisResponse(
    int GoodCount,
    int DefectCount,
    int ItemCount,
    List<ActVariableResult> Variables);

// ---------- 신규 품목 CT 예측 ----------

public record PredictRequest(string Material, double Diameter, double CutLength, int FluteCount);

public record PredictionResult(
    double PredictedCtSec,
    double ExpectedErrorPct,
    double LowCtSec,
    double HighCtSec,
    string? Warning);

public record LooRow(string ItemCode, double StandardCt, double PredictedCt, double ErrorPct);

public record ModelInfo(int NSamples, double Alpha, double LooMapePct, List<string> Materials, List<LooRow> Rows);
