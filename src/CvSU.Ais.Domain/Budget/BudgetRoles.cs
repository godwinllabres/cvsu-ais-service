namespace CvSU.Ais.Domain.Budget;

/// <summary>Roles that gate the budget execution cycle.
///
/// The string VALUE must match the Frappe <c>Role.name</c> (see ais-template
/// <c>constants/roles.py</c>) so the gate and a real identity provider agree. The
/// legacy app gates the budget cycle on <c>AIS Accounting Staff</c> (Budget Division
/// duties live under Accounting Staff today); there is no separate "Budget Officer"
/// Frappe role seeded. Align here rather than inventing a name the directory will
/// never emit.</summary>
public static class BudgetRoles
{
    public const string BudgetOfficer = "AIS Accounting Staff";
}
