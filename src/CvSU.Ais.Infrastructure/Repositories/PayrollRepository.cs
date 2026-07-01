using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.Payroll;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

// ─── Regular Payroll Entry ────────────────────────────────────────────────────

public sealed class PayrollEntryRepository(AisDbContext db) : IPayrollEntryRepository
{
    public async Task<IReadOnlyList<PayrollEntryView>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<PayrollEntryRow>()
            .OrderByDescending(r => r.PostingDate)
            .ThenBy(r => r.Name)
            .Select(r => new PayrollEntryView(
                r.Name,
                r.PayrollType,
                r.PayrollPeriod,
                r.PostingDate,
                r.TotalNetPay,
                r.Status))
            .ToListAsync(cancellationToken);

    public async Task<PayrollEntryDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<PayrollEntryRow>()
            .Include(r => r.LoanDeductions)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        if (row is null)
            return null;

        return ToDetailView(row);
    }

    public async Task<PayrollEntryView> AddAsync(
        CreatePayrollEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var year = command.PostingDate.Year;
        var name = await NextNameAsync("PAY", year, cancellationToken);

        // Persist the payroll register summary figures supplied on import so that
        // downstream validation/posting has real totals to work with (F4).
        var totalOtherDeductions = command.LoanDeductions.Sum(d => d.Amount);
        var totalNetPay = command.TotalGrossPay
            - (command.TotalTaxWithheld
                + command.TotalGsis
                + command.TotalPagibig
                + command.TotalPhilhealth
                + totalOtherDeductions);

        var row = new PayrollEntryRow
        {
            Name = name,
            PayrollType = command.PayrollType,
            PayrollPeriod = command.PayrollPeriod,
            PostingDate = command.PostingDate,
            FundCluster = command.FundCluster,
            ImportStatus = command.TotalGrossPay > 0m ? "Imported" : "NotImported",
            TotalRecords = command.TotalRecords,
            TotalGrossPay = command.TotalGrossPay,
            TotalNetPay = totalNetPay,
            TotalTaxWithheld = command.TotalTaxWithheld,
            TotalGsis = command.TotalGsis,
            TotalPagibig = command.TotalPagibig,
            TotalPhilhealth = command.TotalPhilhealth,
            TotalOtherDeductions = totalOtherDeductions,
            Status = "Draft",
            Remarks = command.Remarks,
            LoanDeductions = command.LoanDeductions
                .Select(d => new PayrollLoanDeductionRow
                {
                    ParentPayrollName = name,
                    LoanType = d.LoanType,
                    LoanReference = d.LoanReference,
                    EmployeeId = d.EmployeeId,
                    EmployeeName = d.EmployeeName,
                    Amount = d.Amount,
                })
                .ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return new PayrollEntryView(row.Name, row.PayrollType, row.PayrollPeriod, row.PostingDate, row.TotalNetPay, row.Status);
    }

    public async Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<PayrollEntryRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll entry '{name}' not found for update.");

        row.Status = status;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<PayrollEntryRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Payroll entry '{name}' not found.");
        row.GlPostingReference = glRef;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> NextNameAsync(string prefix, int year, CancellationToken cancellationToken)
    {
        var seriesKey = $"{prefix}-{year}";
        var existing = await db.Set<PayrollEntryRow>()
            .CountAsync(r => r.Name.StartsWith(seriesKey + "-"), cancellationToken);
        var seq = existing + 1;
        return $"{seriesKey}-{seq:D5}";
    }

    private static PayrollEntryDetailView ToDetailView(PayrollEntryRow row) => new(
        row.Name,
        row.Title,
        row.PayrollType,
        row.PayrollPeriod,
        row.PostingDate,
        row.FundCluster,
        row.ImportStatus,
        row.TotalRecords,
        row.TotalGrossPay,
        row.TotalNetPay,
        row.TotalTaxWithheld,
        row.TotalGsis,
        row.TotalPagibig,
        row.TotalPhilhealth,
        row.TotalOtherDeductions,
        row.Status,
        row.GlPostingReference,
        row.ValidationErrors,
        row.Remarks,
        row.LoanDeductions
            .Select(d => new PayrollLoanDeductionDto(d.LoanType, d.LoanReference, d.EmployeeId, d.EmployeeName, d.Amount))
            .ToList());
}

// ─── JO/COS Payroll Entry ─────────────────────────────────────────────────────

public sealed class JoCosPayrollRepository(AisDbContext db) : IJoCosPayrollRepository
{
    public async Task<IReadOnlyList<JoCosPayrollView>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<JoCosPayrollEntryRow>()
            .OrderByDescending(r => r.PostingDate)
            .ThenBy(r => r.Name)
            .Select(r => new JoCosPayrollView(
                r.Name,
                r.EmployeeType,
                r.PayrollPeriod,
                r.TotalGross,
                r.TotalNet,
                r.Status))
            .ToListAsync(cancellationToken);

    public async Task<JoCosPayrollDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<JoCosPayrollEntryRow>()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        if (row is null)
            return null;

        return ToDetailView(row);
    }

    public async Task<JoCosPayrollView> AddAsync(
        CreateJoCosPayrollCommand command,
        CancellationToken cancellationToken = default)
    {
        var year = command.PostingDate.Year;
        var name = await NextNameAsync(year, cancellationToken);

        var lines = command.Lines
            .Select(l => new JoCosPayrollLineRow
            {
                ParentJoCosName = name,
                EmployeeId = l.EmployeeId,
                EmployeeName = l.EmployeeName,
                EmploymentType = l.EmploymentType,
                AuthorizedHours = l.AuthorizedHours,
                ActualHours = l.ActualHours,
                TardinessHours = l.TardinessHours,
                ComputedHours = l.ActualHours - l.TardinessHours,
                DailyRate = l.DailyRate,
                GrossPay = l.GrossPay,
                NetPay = l.NetPay,
                DiscrepancyFlag = false,
                Remarks = l.Remarks,
            })
            .ToList();

        var row = new JoCosPayrollEntryRow
        {
            Name = name,
            EmployeeType = command.EmployeeType,
            PayrollPeriod = command.PayrollPeriod,
            PeriodFrom = command.PeriodFrom,
            PeriodTo = command.PeriodTo,
            PostingDate = command.PostingDate,
            FundCluster = command.FundCluster,
            TotalHours = lines.Sum(l => l.ComputedHours),
            TotalDays = 0m,
            TotalGross = lines.Sum(l => l.GrossPay),
            TotalNet = lines.Sum(l => l.NetPay),
            HoursValidated = false,
            DtrValidated = false,
            AccomplishmentValidated = false,
            Status = "Draft",
            Remarks = command.Remarks,
            Lines = lines,
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return new JoCosPayrollView(row.Name, row.EmployeeType, row.PayrollPeriod, row.TotalGross, row.TotalNet, row.Status);
    }

    public async Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<JoCosPayrollEntryRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"JO/COS payroll entry '{name}' not found for update.");

        row.Status = status;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<JoCosPayrollEntryRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");
        row.GlPostingReference = glRef;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLineComputedAsync(
        string parentName, string employeeId,
        decimal computedHours, decimal grossPay, decimal netPay,
        CancellationToken cancellationToken = default)
    {
        var line = await db.Set<JoCosPayrollLineRow>()
            .FirstOrDefaultAsync(
                r => r.ParentJoCosName == parentName && r.EmployeeId == employeeId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                $"JO/COS line for employee '{employeeId}' in payroll '{parentName}' not found.");

        line.ComputedHours = computedHours;
        line.GrossPay = grossPay;
        line.NetPay = netPay;
        line.DiscrepancyFlag = computedHours != line.AuthorizedHours;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateComputedTotalsAsync(
        string name, decimal totalHours, decimal totalDays,
        decimal totalGross, decimal totalNet,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<JoCosPayrollEntryRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");

        row.TotalHours = totalHours;
        row.TotalDays = totalDays;
        row.TotalGross = totalGross;
        row.TotalNet = totalNet;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> NextNameAsync(int year, CancellationToken cancellationToken)
    {
        var seriesKey = $"JOPAY-{year}";
        var existing = await db.Set<JoCosPayrollEntryRow>()
            .CountAsync(r => r.Name.StartsWith(seriesKey + "-"), cancellationToken);
        var seq = existing + 1;
        return $"{seriesKey}-{seq:D5}";
    }

    private static JoCosPayrollDetailView ToDetailView(JoCosPayrollEntryRow row) => new(
        row.Name,
        row.EmployeeType,
        row.PayrollPeriod,
        row.PeriodFrom,
        row.PeriodTo,
        row.PostingDate,
        row.FundCluster,
        row.HrTransmittalReference,
        row.HrTransmittalReceivedDate,
        row.TotalHours,
        row.TotalDays,
        row.TotalGross,
        row.TotalNet,
        row.HoursValidated,
        row.DtrValidated,
        row.AccomplishmentValidated,
        row.ValidationRemarks,
        row.Status,
        row.GlPostingReference,
        row.Remarks,
        row.Lines
            .Select(l => new JoCosPayrollLineDto(
                l.EmployeeId, l.EmployeeName, l.EmploymentType,
                l.AuthorizedHours, l.ActualHours, l.TardinessHours,
                l.DailyRate, l.GrossPay, l.NetPay, l.Remarks))
            .ToList());
}

// ─── Salary Tranche ───────────────────────────────────────────────────────────

public sealed class SalaryTrancheRepository(AisDbContext db) : ISalaryTrancheRepository
{
    public async Task<IReadOnlyList<SalaryTrancheView>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<SalaryTrancheRow>()
            .OrderByDescending(r => r.EffectiveYear)
            .ThenBy(r => r.TrancheNumber)
            .Select(r => new SalaryTrancheView(
                r.Name,
                r.SslLaw,
                r.TrancheNumber,
                r.EffectiveYear,
                r.IsActive,
                r.Status))
            .ToListAsync(cancellationToken);

    public async Task<SalaryTrancheDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<SalaryTrancheRow>()
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        if (row is null)
            return null;

        return ToDetailView(row);
    }

    public async Task<SalaryTrancheView> AddAsync(
        CreateSalaryTrancheCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = await NextNameAsync(command.EffectiveYear, cancellationToken);

        var entries = (command.Entries ?? [])
            .Select(e => new SalaryTrancheEntryRow
            {
                ParentTrancheName = name,
                SalaryGrade = e.SalaryGrade,
                Step = e.Step,
                MonthlySalary = e.MonthlySalary,
            })
            .ToList();

        var row = new SalaryTrancheRow
        {
            Name = name,
            SslLaw = command.SslLaw,
            TrancheNumber = command.TrancheNumber,
            EffectiveYear = command.EffectiveYear,
            EffectiveDate = command.EffectiveDate,
            DbmCircularReference = command.DbmCircularReference,
            IsActive = false,
            TotalEntries = entries.Count,
            MinSalary = entries.Count > 0 ? entries.Min(e => e.MonthlySalary) : 0m,
            MaxSalary = entries.Count > 0 ? entries.Max(e => e.MonthlySalary) : 0m,
            Status = "Draft",
            ImportStatus = "NotImported",
            Remarks = command.Remarks,
            Entries = entries,
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return new SalaryTrancheView(row.Name, row.SslLaw, row.TrancheNumber, row.EffectiveYear, row.IsActive, row.Status);
    }

    public async Task UpdateStatusAsync(
        string name,
        string status,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<SalaryTrancheRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Salary tranche '{name}' not found for update.");

        row.Status = status;
        row.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> NextNameAsync(int effectiveYear, CancellationToken cancellationToken)
    {
        var seriesKey = $"ST-{effectiveYear}";
        var existing = await db.Set<SalaryTrancheRow>()
            .CountAsync(r => r.Name.StartsWith(seriesKey + "-"), cancellationToken);
        var seq = existing + 1;
        return $"{seriesKey}-{seq:D5}";
    }

    private static SalaryTrancheDetailView ToDetailView(SalaryTrancheRow row) => new(
        row.Name,
        row.SslLaw,
        row.TrancheNumber,
        row.EffectiveYear,
        row.EffectiveDate,
        row.DbmCircularReference,
        row.IsActive,
        row.TotalEntries,
        row.MinSalary,
        row.MaxSalary,
        row.Status,
        row.ImportStatus,
        row.ValidationErrors,
        row.Remarks,
        row.Entries
            .OrderBy(e => e.SalaryGrade)
            .ThenBy(e => e.Step)
            .Select(e => new SalaryTrancheEntryDto(e.SalaryGrade, e.Step, e.MonthlySalary))
            .ToList());
}
