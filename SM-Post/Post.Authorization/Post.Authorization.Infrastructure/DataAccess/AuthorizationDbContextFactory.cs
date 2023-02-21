using System;
using Microsoft.EntityFrameworkCore;

namespace Post.Authorization.Infrastructure.DataAccess
{
	public class AuthorizationDbContextFactory
	{
        private readonly Action<DbContextOptionsBuilder> _ConfigureDbContext;

        public AuthorizationDbContextFactory(Action<DbContextOptionsBuilder> configureDbContext)
        {
            _ConfigureDbContext = configureDbContext;
        }

        public AuthorizationDbContext CreateDbContext()
        {
            DbContextOptionsBuilder<AuthorizationDbContext> optionsBuilder = new();
            _ConfigureDbContext(optionsBuilder);

            return new AuthorizationDbContext(optionsBuilder.Options);
        }
    }
}

