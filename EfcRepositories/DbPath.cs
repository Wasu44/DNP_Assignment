namespace EfcRepositories;

/// <summary>
/// Finds app.db inside the EfcRepositories folder, no matter if the app is started
/// from EfcRepositories (dotnet ef ...) or from WebAPI (dotnet run / Rider).
/// This way no absolute path is hard-coded, so it works for every group member.
/// </summary>
public static class DbPath
{
    private const string FileName = "app.db";

    public static string Resolve()
    {
        DirectoryInfo current = new(Directory.GetCurrentDirectory());

        // Started from the EfcRepositories folder itself (e.g. dotnet ef database update)
        if (File.Exists(Path.Combine(current.FullName, "EfcRepositories.csproj")))
        {
            return Path.Combine(current.FullName, FileName);
        }

        // Started from a sibling project, e.g. WebAPI -> ../EfcRepositories/app.db
        if (current.Parent is not null)
        {
            string sibling = Path.Combine(current.Parent.FullName, "EfcRepositories");
            if (Directory.Exists(sibling))
            {
                return Path.Combine(sibling, FileName);
            }
        }

        return Path.Combine(current.FullName, FileName);
    }
}
