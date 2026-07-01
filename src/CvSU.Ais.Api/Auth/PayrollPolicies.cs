namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy names for payroll (regular and JO/COS) and salary
/// tranches. Payroll posts to the accrual GL, so posting is gated to the Accountant.</summary>
public static class PayrollPolicies
{
    public const string Manage = "payroll:manage";
    public const string Post = "payroll:post";
    public const string ManageTranche = "salary-tranche:manage";
}
