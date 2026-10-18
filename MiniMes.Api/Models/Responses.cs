namespace MiniMes.Api.Models;

// ---------- Cycle Time 표준화 ----------

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

public record StandardComparisonRow(
    string ItemCode,
    string Name,
    double? StandardCt,
    double? RecentAvgCt,
    int RecentCount,
    double? DeviationPct,
    string Judgement);

// ---------- ACT 분석 ----------

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

// 예측 서버(Python)의 /predict 응답
public record PredictorResult(
    double PredictedCtSec,
    double ExpectedErrorPct,
    double LowCtSec,
    double HighCtSec,
    string? Warning);

// 표준 CT가 아직 없는 등록 품목의 예측 결과
public record NewItemPrediction(
    string ItemCode,
    string Name,
    string Material,
    double Diameter,
    double CutLength,
    int FluteCount,
    double PredictedCtSec,
    double ExpectedErrorPct,
    string? Warning);

// ---------- 작업자 화면(설비 현황) ----------

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
