using System.ComponentModel.DataAnnotations;

namespace MiniMes.Api.Models;

// 클라이언트가 보내도 되는 값만 담은 요청 전용 객체(DTO)
public record CreateLotRequest(
    [Required, StringLength(30)] string LotNo,
    [Range(1, 100000)] int Quantity);

public record CreateResultRequest(
    [Required, StringLength(30)] string ProcessCode,
    [Required, StringLength(30)] string EquipmentCode,
    [Range(0, 100000)] int GoodQty,
    [Range(0, 100000)] int DefectQty,
    [Range(0.1, 100000)] double CycleTimeSec);

public record CreateEquipmentLogRequest(
    [Required, StringLength(30)] string EquipmentCode,
    EquipmentStatus Status,
    [Range(0, 100000)] double SpindleRpm,
    [Range(0, 100000)] double FeedRate,
    [Range(0, 200)] double SpindleLoadPct);

// 신규 품목 CT 예측 요청 (품목 사양)
public record PredictRequest(
    [Required, StringLength(20)] string Material,
    [Range(0.1, 500)] double Diameter,
    [Range(0.1, 1000)] double CutLength,
    [Range(1, 12)] int FluteCount);

public record CreateItemRequest(
    [Required, StringLength(30)] string ItemCode,
    [Required, StringLength(100)] string Name,
    [Required, StringLength(20)] string Material,
    [Range(0.1, 500)] double Diameter,
    [Range(0.1, 1000)] double CutLength,
    [Range(1, 12)] int FluteCount);

public record CreateCycleRequest(
    [Required, StringLength(30)] string ItemCode,
    [Required, StringLength(30)] string EquipmentCode,
    string? LotNo,
    DateTime StartedAt,
    [Range(1, 100000)] double CycleTimeSec,
    bool IsGood,
    [Range(0, 500)] double SpindleLoadAvg,
    [Range(0, 500)] double SpindleLoadMax,
    [Range(0, 100000)] double SpindleRpmAvg,
    [Range(0, 100000)] double FeedRateAvg,
    [Range(0, 1000)] double VibrationMax);
