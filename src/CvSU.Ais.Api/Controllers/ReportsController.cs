using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

/// <summary>Read-only COA financial registries, computed off the two ledgers.
/// Every endpoint defaults to the current fiscal year when none is supplied.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = ReportPolicies.View)]
public sealed class ReportsController(ReportingService reports) : ControllerBase
{
    /// <summary>RAOD (clusters 01–04) and RBUD (05–06): allotment, obligation,
    /// disbursement and balances per fund cluster + expense class.</summary>
    [HttpGet("budget-registry")]
    public async Task<ActionResult<BudgetRegistryReport>> BudgetRegistry(
        [FromQuery] int? fiscalYear, CancellationToken cancellationToken) =>
        Ok(await reports.BudgetRegistryAsync(Year(fiscalYear), cancellationToken));

    /// <summary>RAPAL: appropriation vs allotment per fund cluster + expense class.</summary>
    [HttpGet("rapal")]
    public async Task<ActionResult<AppropriationAllotmentReport>> Rapal(
        [FromQuery] int? fiscalYear, CancellationToken cancellationToken) =>
        Ok(await reports.AppropriationAllotmentAsync(Year(fiscalYear), cancellationToken));

    /// <summary>Trial balance: GL debit/credit totals per account; the report
    /// reports whether the whole book balances.</summary>
    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReport>> TrialBalance(
        [FromQuery] int? fiscalYear, CancellationToken cancellationToken) =>
        Ok(await reports.TrialBalanceAsync(Year(fiscalYear), cancellationToken));

    /// <summary>The four General-Purpose Financial Statements (GAM/PPSAS): Statement of
    /// Financial Position, Financial Performance, Changes in Net Assets/Equity and Cash Flows,
    /// derived from the classified GL account balances.</summary>
    [HttpGet("financial-statements")]
    public async Task<ActionResult<FinancialStatementsReport>> FinancialStatements(
        [FromQuery] int? fiscalYear, CancellationToken cancellationToken) =>
        Ok(await reports.FinancialStatementsAsync(Year(fiscalYear), cancellationToken));

    /// <summary>RROR / Quarterly Report of Revenue and Other Receipts (FAR No. 5): revenue
    /// recognised per account, split by quarter.</summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReport>> Revenue(
        [FromQuery] int? fiscalYear, CancellationToken cancellationToken) =>
        Ok(await reports.RevenueReportAsync(Year(fiscalYear), cancellationToken));

    /// <summary>Monthly Report of Disbursements (FAR No. 4): cash disbursements per month.</summary>
    [HttpGet("monthly-disbursements")]
    public async Task<ActionResult<MonthlyDisbursementReport>> MonthlyDisbursements(
        [FromQuery] int? fiscalYear, CancellationToken cancellationToken) =>
        Ok(await reports.MonthlyDisbursementsAsync(Year(fiscalYear), cancellationToken));

    private static int Year(int? fiscalYear) => fiscalYear ?? DateTime.UtcNow.Year;
}
