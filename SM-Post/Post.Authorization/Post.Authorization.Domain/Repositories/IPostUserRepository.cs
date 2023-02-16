using System;
using Post.Authorization.Domain.Entities;

namespace Post.Authorization.Domain.Repositories
{
	public interface IPostUserRepository
	{
		Task CreateUserAsync(PostUser postUser);
        Task UpdateUserAsync(PostUser postUser);
        Task DeleteUserAsync(string postUserId);
        Task<List<PostUser>> GetlistOfUsers();
		Task<PostUser> GetUserByUserName(string userName);
        Task<PostUser> GetUserByUserId(string userId);
    }
}

