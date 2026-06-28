using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.CashAdvances;
using CvSU.Ais.Application.Collections;
using CvSU.Ais.Application.Compliance;
using CvSU.Ais.Application.Exports;
using CvSU.Ais.Application.JournalEntries;
using CvSU.Ais.Application.Obligations;
using CvSU.Ais.Application.Payroll;
using CvSU.Ais.Application.Payments;
using CvSU.Ais.Application.ReferenceData;
using CvSU.Ais.Application.Routing;
using CvSU.Ais.Infrastructure.Interceptors;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CvSU.Ais.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the EF Core context (with the immutability interceptor)
    /// and the gapless voucher-number service.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<LedgerImmutabilityInterceptor>();

        services.AddDbContext<AisDbContext>((sp, options) =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<LedgerImmutabilityInterceptor>()));

        // ── Core infrastructure ───────────────────────────────────────────────
        services.AddScoped<IVoucherNumberGenerator, GaplessVoucherNumberService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IFundingSourceCatalog, FundingSourceCatalog>();
        services.AddScoped<IGeneralLedger, GeneralLedgerRepository>();
        services.AddScoped<IBudgetLedger, BudgetLedgerRepository>();
        services.AddScoped<IReportingQueries, ReportingQueries>();
        services.AddScoped<DevDataSeeder>();

        // ── Disbursement Vouchers ─────────────────────────────────────────────
        services.AddScoped<IDisbursementVoucherRepository, DisbursementVoucherRepository>();

        // ── Journal Entries ───────────────────────────────────────────────────
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        services.AddScoped<JournalEntryService>();

        // ── Reference Data ────────────────────────────────────────────────────
        services.AddScoped<IPapCodeRepository, PapCodeRepository>();
        services.AddScoped<ILocationCodeRepository, LocationCodeRepository>();
        services.AddScoped<IOperationalFundRepository, OperationalFundRepository>();
        services.AddScoped<PapCodeService>();
        services.AddScoped<LocationCodeService>();
        services.AddScoped<OperationalFundService>();

        // ── Obligations ───────────────────────────────────────────────────────
        services.AddScoped<IObligationRequestRepository, ObligationRequestRepository>();
        services.AddScoped<INcaRepository, NcaRepository>();
        services.AddScoped<ObligationRequestService>();
        services.AddScoped<NcaService>();

        // ── Collections ───────────────────────────────────────────────────────
        services.AddScoped<IOrderOfPaymentRepository, OrderOfPaymentRepository>();
        services.AddScoped<IOfficialReceiptRepository, OfficialReceiptRepository>();
        services.AddScoped<IRcdRepository, RcdRepository>();
        services.AddScoped<OrderOfPaymentService>();
        services.AddScoped<OfficialReceiptService>();
        services.AddScoped<RcdService>();

        // ── Cash Advances ─────────────────────────────────────────────────────
        services.AddScoped<ICashAdvanceRepository, CashAdvanceRepository>();
        services.AddScoped<ILiquidationReportRepository, CashAdvanceRepository>();
        services.AddScoped<CashAdvanceService>();
        services.AddScoped<LiquidationReportService>();

        // ── Payroll ───────────────────────────────────────────────────────────
        services.AddScoped<IPayrollEntryRepository, PayrollEntryRepository>();
        services.AddScoped<IJoCosPayrollRepository, JoCosPayrollRepository>();
        services.AddScoped<ISalaryTrancheRepository, SalaryTrancheRepository>();
        services.AddScoped<PayrollEntryService>();
        services.AddScoped<JoCosPayrollService>();
        services.AddScoped<SalaryTrancheService>();

        // ── Payments ──────────────────────────────────────────────────────────
        services.AddScoped<ILddapAdaRepository, LddapAdaRepository>();
        services.AddScoped<IDvTransmittalRepository, DvTransmittalRepository>();
        services.AddScoped<IAuditIntakeRepository, AuditIntakeRepository>();
        services.AddScoped<LddapAdaService>();
        services.AddScoped<DvTransmittalService>();
        services.AddScoped<AuditIntakeService>();

        // ── Exports ───────────────────────────────────────────────────────────
        services.AddScoped<IFindesExportRepository, FindesExportRepository>();
        services.AddScoped<IBankCollectionReportRepository, BankCollectionReportRepository>();
        services.AddScoped<IPushTokenRepository, PushTokenRepository>();
        services.AddScoped<FindesExportService>();
        services.AddScoped<BankCollectionReportService>();
        services.AddScoped<PushTokenService>();

        // ── Routing ───────────────────────────────────────────────────────────
        services.AddScoped<IRoutingTemplateRepository, RoutingTemplateRepository>();
        services.AddScoped<IRoutingSlipRepository, RoutingSlipRepository>();
        services.AddScoped<IAttachmentRequirementRepository, AttachmentRequirementRepository>();
        services.AddScoped<RoutingTemplateService>();
        services.AddScoped<RoutingSlipService>();
        services.AddScoped<AttachmentRequirementService>();

        // ── Compliance ────────────────────────────────────────────────────────
        services.AddScoped<ICoaCaseRepository, CoaCaseRepository>();
        services.AddScoped<IBir2307Repository, Bir2307Repository>();
        services.AddScoped<IWhtStatementRepository, WhtStatementRepository>();
        services.AddScoped<IStateHistoryRepository, StateHistoryRepository>();
        services.AddScoped<CoaCaseService>();
        services.AddScoped<Bir2307Service>();
        services.AddScoped<WhtStatementService>();
        services.AddScoped<StateHistoryService>();

        return services;
    }
}
