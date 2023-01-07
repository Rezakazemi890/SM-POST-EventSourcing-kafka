using Microsoft.EntityFrameworkCore;
using Post.Query.Domain.Entities;
using Post.Query.Domain.Repositories;
using Post.Query.Infrastructure.DataAccess;

namespace Post.Query.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly DatabaseContextFactory _contextFactory;

    public PostRepository(DatabaseContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task CreateAsync(PostEntity post)
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();
        context.Posts.Add(post);

        _ = await context.SaveChangesAsync();
    }


    public async Task DeleteAsync(Guid postId)
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();
        var post = await GetByIdAsync(postId);

        if (post == null) return;

        context.Posts.Remove(post);
        _ = await context.SaveChangesAsync();
    }

    public async Task<PostEntity> GetByIdAsync(Guid postId)
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();

        return await context.Posts
                .Include(x => x.Comments)
                .FirstOrDefaultAsync(y => y.PostId == postId);
    }

    public async Task<List<PostEntity>> ListAllAsync()
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();

        return await context.Posts.AsNoTracking()
            .Include(x => x.Comments).AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<PostEntity>> ListByAuthorAsync(string author)
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();

        return await context.Posts.AsNoTracking()
            .Include(x => x.Comments).AsNoTracking()
            .Where(y => y.Author.Contains(author))
            .ToListAsync();
    }

    public async Task<List<PostEntity>> ListWithCommentsAsync()
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();

        return await context.Posts.AsNoTracking()
            .Include(x => x.Comments).AsNoTracking()
            .Where(y => y.Comments != null && y.Comments.Any())
            .ToListAsync();
    }

    public async Task<List<PostEntity>> ListWithLikesAsync(int numberOflikes)
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();

        return await context.Posts.AsNoTracking()
            .Include(x => x.Comments).AsNoTracking()
            .Where(y => y.Likes >= numberOflikes)
            .ToListAsync();
    }

    public async Task UpdateAsync(PostEntity post)
    {
        using DatabaseContext context = _contextFactory.CreateDbContext();

        context.Posts.Update(post);

        _ = await context.SaveChangesAsync();
    }
}
