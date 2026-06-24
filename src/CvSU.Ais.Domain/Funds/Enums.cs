namespace CvSU.Ais.Domain.Funds;

/// <summary>Which budget registry a fund cluster reports through.</summary>
public enum RegistryType
{
    /// <summary>Trust receipts (cluster 07) keep no RAOD/RBUD.</summary>
    None,

    /// <summary>Registry of Allotments, Obligations and Disbursements (clusters 01–04).</summary>
    Raod,

    /// <summary>Registry of Budget, Utilization and Disbursements (clusters 05–06).</summary>
    Rbud,
}

/// <summary>The four object-of-expenditure classes every budget line resolves to.</summary>
public enum ExpenseClass
{
    /// <summary>Personnel Services.</summary>
    Ps,

    /// <summary>Maintenance and Other Operating Expenses.</summary>
    Mooe,

    /// <summary>Financial Expenses.</summary>
    Fe,

    /// <summary>Capital Outlays.</summary>
    Co,
}
