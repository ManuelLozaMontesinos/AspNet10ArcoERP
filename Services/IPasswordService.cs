namespace AspNet10ArcoERP.Services
{
    public interface IPasswordService
    {
        bool VerifyPassword(string inputPassword, string dbPasswordBase64, byte[] dbPublicKey);
        string EncryptPassword(string plainPassword, out byte[] publicKey);
    }
}
