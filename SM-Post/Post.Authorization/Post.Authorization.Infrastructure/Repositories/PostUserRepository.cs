using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Hosting;
using Post.Authorization.Domain.Entities;
using Post.Authorization.Domain.Repositories;
using Post.Authorization.Infrastructure.DataAccess;

namespace Post.Authorization.Infrastructure.Repositories
{
    public class PostUserRepository : IPostUserRepository
    {
        private readonly AuthorizationDbContextFactory _dbContextFactory;

        public PostUserRepository(AuthorizationDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task CreateUserAsync(PostUser postUser)
        {
            if (await GetUserByUserName(postUser.UserName) != null)
                throw new InvalidDataException("A user with the entered username exists!");
            using AuthorizationDbContext context = _dbContextFactory.CreateDbContext();
            context.Users.Add(postUser);

            _ = await context.SaveChangesAsync();            
        }

        public async Task DeleteUserAsync(string postUserId)
        {
            using AuthorizationDbContext context = _dbContextFactory.CreateDbContext();
            PostUser postUser = await GetUserByUserId(postUserId);

            if (postUser == null) return;

            context.Users.Remove(postUser);
            _ = await context.SaveChangesAsync();
        }

        public async Task<List<PostUser>> GetlistOfUsers()
        {
            using AuthorizationDbContext context = _dbContextFactory.CreateDbContext();
            return await context.Users.AsNoTracking().ToListAsync();
        }

        public async Task<PostUser> GetUserByUserId(string userId)
        {
            using AuthorizationDbContext context = _dbContextFactory.CreateDbContext();

            return await context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<PostUser> GetUserByUserName(string userName)
        {
            using AuthorizationDbContext context = _dbContextFactory.CreateDbContext();

            return await context.Users
                .FirstOrDefaultAsync(x => x.UserName == userName);
        }

        public async Task UpdateUserAsync(PostUser postUser)
        {
            using AuthorizationDbContext context = _dbContextFactory.CreateDbContext();

            context.Users.Update(postUser);
            _ = await context.SaveChangesAsync();
        }
    }
}

