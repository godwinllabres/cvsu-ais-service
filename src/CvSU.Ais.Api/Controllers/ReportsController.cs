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

    private static int Year(int? fiscalYear) => fiscalYear ?? DateTime.UtcNow.Year;
}
