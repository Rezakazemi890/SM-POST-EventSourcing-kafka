using CQRS.Core.Queries;

namespace Post.Query.Api.Queries;

public class FindPostWithAuthorQuery : BaseQuery
{
    public string Author { get; set; }
}
