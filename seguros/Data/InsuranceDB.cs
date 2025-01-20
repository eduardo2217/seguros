using Microsoft.EntityFrameworkCore;
using seguros.Models;

namespace seguros.Data
{
    public class InsuranceDB : DbContext
    {
        public InsuranceDB(DbContextOptions<InsuranceDB> options) : base(options)
        {

        }
        public DbSet<Insured> insureds => Set<Insured>();
    }

}
