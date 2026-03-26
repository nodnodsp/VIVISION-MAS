using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IReportExportRepository
{
    Task AddAsync(ReportExport export, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportExport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportExport>> GetByRecordIdAsync(string recordId, CancellationToken cancellationToken = default);
}
