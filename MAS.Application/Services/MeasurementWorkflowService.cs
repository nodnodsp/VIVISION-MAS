using MAS.Application.Abstractions;
using MAS.Application.Models;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MeasurementTaskStatus = MAS.Core.Enums.TaskStatus;

namespace MAS.Application.Services;

public sealed class MeasurementWorkflowService : IMeasurementWorkflowService
{
    private readonly IMeasurementTaskRepository _taskRepository;
    private readonly IMeasurementRecordRepository _recordRepository;
    private readonly IMeasurementAngleResultRepository _angleResultRepository;
    private readonly IMeasurementEffectResultRepository _effectResultRepository;
    private readonly IInstrumentMeasurementService _instrumentMeasurementService;

    public MeasurementWorkflowService(
        IMeasurementTaskRepository taskRepository,
        IMeasurementRecordRepository recordRepository,
        IMeasurementAngleResultRepository angleResultRepository,
        IMeasurementEffectResultRepository effectResultRepository,
        IInstrumentMeasurementService instrumentMeasurementService)
    {
        _taskRepository = taskRepository;
        _recordRepository = recordRepository;
        _angleResultRepository = angleResultRepository;
        _effectResultRepository = effectResultRepository;
        _instrumentMeasurementService = instrumentMeasurementService;
    }

    public async Task<MeasurementExecutionResult> ExecuteMeasurementAsync(string taskCode, string recordType, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByCodeAsync(taskCode, cancellationToken)
                   ?? throw new InvalidOperationException($"任务不存在: {taskCode}");

        var existingRecords = await _recordRepository.GetByTaskIdAsync(task.Id, cancellationToken);
        var sequenceNo = existingRecords.Count + 1;

        task.Status = MeasurementTaskStatus.Running;
        task.StartedAt ??= DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        await _taskRepository.UpdateAsync(task, cancellationToken);

        try
        {
            var measurement = await _instrumentMeasurementService.MeasureAsync(new InstrumentMeasurementRequest
            {
                TaskId = task.Id,
                InstrumentId = task.InstrumentId,
                TaskCode = task.TaskCode,
                TaskType = task.TaskType,
                RecordType = recordType,
                MeasurementMode = task.MeasurementMode,
                SequenceNo = sequenceNo,
            }, cancellationToken);

            var now = DateTime.UtcNow;
            var record = new MeasurementRecord
            {
                TaskId = task.Id,
                RecordNo = sequenceNo,
                RecordType = recordType,
                TotalDeltaE = measurement.TotalDeltaE,
                TotalEffectDiff = measurement.TotalEffectDiff,
                PassStatus = measurement.PassStatus,
                ResultSummary = measurement.ResultSummary,
                MeasuredAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var angleResults = measurement.AngleResults.Select(item => new MeasurementAngleResult
            {
                RecordId = record.Id,
                AngleCode = item.AngleCode,
                CieL = item.CieL,
                CieA = item.CieA,
                CieB = item.CieB,
                DeltaE = item.DeltaE,
                PassStatus = item.PassStatus,
                CreatedAt = now,
                UpdatedAt = now,
            }).ToList();

            var effectResults = measurement.EffectResults.Select(item => new MeasurementEffectResult
            {
                RecordId = record.Id,
                AngleCode = item.AngleCode,
                SparkleValue = item.SparkleValue,
                SparkleDiff = item.SparkleDiff,
                GraininessValue = item.GraininessValue,
                GraininessDiff = item.GraininessDiff,
                EffectPassStatus = item.PassStatus,
                CreatedAt = now,
                UpdatedAt = now,
            }).ToList();

            await _recordRepository.AddAsync(record, cancellationToken);
            await _angleResultRepository.AddRangeAsync(angleResults, cancellationToken);
            await _effectResultRepository.AddRangeAsync(effectResults, cancellationToken);

            task.Status = MeasurementTaskStatus.Completed;
            task.FinishedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task, cancellationToken);

            return new MeasurementExecutionResult
            {
                Task = task,
                Record = record,
                AngleResults = angleResults,
                EffectResults = effectResults,
            };
        }
        catch
        {
            task.Status = MeasurementTaskStatus.Failed;
            task.UpdatedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task, cancellationToken);
            throw;
        }
    }
}

