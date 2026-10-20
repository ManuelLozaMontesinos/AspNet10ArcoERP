using System.Data;

namespace AspNet10ArcoERP.Services
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
