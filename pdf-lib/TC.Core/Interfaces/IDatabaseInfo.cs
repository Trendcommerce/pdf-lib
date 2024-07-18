namespace TC.Interfaces
{
    // Database-Info (03.02.2024, SME)
    public interface IDatabaseInfo
    {
        string ServerName { get; }
        string DatabaseName { get; }
    }
}
