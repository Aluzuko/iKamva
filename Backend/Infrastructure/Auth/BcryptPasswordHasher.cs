namespace Backend.Infrastructure.Auth;

public class BcryptPasswordHasher : IPasswordHasher
{
    // Work factor 12 is a reasonable balance of security vs. login latency as of 2026.
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
