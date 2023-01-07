using CQRS.Core.Domain;
using Post.Common.Events;

namespace Post.Cmd.Domain.Aggregates;

public class PostAggregate : AggregateRoot
{
    private bool _active;
    private string _author;
    private readonly Dictionary<Guid, Tuple<string, string>> _comments = new();
    public bool Active
    {
        get => _active; set => _active = value;
    }

    public PostAggregate()
    {

    }

    public PostAggregate(Guid id, string author, string message)
    {
        RaiseEvent(new PostCreatedEvent
        {
            Id = id,
            Author = author,
            DatePosted = DateTime.Now,
            Message = message
        }
        );
    }

    public void Apply(PostCreatedEvent @event)
    {
        _id = @event.Id;
        _active = true;
        _author = @event.Author;
    }
    public void EditMessage(string message)
    {
        if (!_active)
        {
            throw new InvalidOperationException("You Can't Edit Message Of Inactive Post!");
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException($"The Value Of {nameof(message)} Can't Be Null Or Empty!");
        }

        RaiseEvent(new MessageUpdatedEvent
        {
            Id = _id,
            Message = message
        });
    }

    public void Apply(MessageUpdatedEvent @event)
    {
        _id = @event.Id;
    }

    public void LikePost()
    {
        if (!_active)
        {
            throw new InvalidOperationException("You Can't Like Post Of Inactive Post!");
        }
        RaiseEvent(new PostLikedEvent
        {
            Id = _id
        });
    }
    public void Apply(PostLikedEvent @event)
    {
        _id = @event.Id;
    }

    public void AddComment(string comment, string userName)
    {
        if (!_active)
        {
            throw new InvalidOperationException("You Can't Add Comment Of Inactive Post!");
        }
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new InvalidOperationException($"The Value Of {nameof(comment)} Can't Be Null Or Empty!");
        }
        RaiseEvent(new CommentAddedEvent
        {
            Comment = comment,
            Username = userName,
            CommentDate = DateTime.Now,
            CommentId = Guid.NewGuid(),
            Id = _id
        });
    }

    public void Apply(CommentAddedEvent @event)
    {
        _id = @event.Id;
        _comments.Add(@event.CommentId, new Tuple<string, string>(@event.Comment, @event.Username));
    }

    public void EditComment(string comment, Guid commentId, string userName)
    {
        if (!_active)
        {
            throw new InvalidOperationException("You Can't Edit Comment Of Inactive Post!");
        }
        if (!_comments[commentId].Item2.Equals(userName, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new InvalidOperationException("You Aren't Allowed To Edit A Comment That Was Made By Another User!");
        }

        RaiseEvent(new CommentUpdatedEvent
        {
            Id = _id,
            CommentId = commentId,
            Comment = comment,
            Username = userName,
            EditDate = DateTime.Now
        });
    }

    public void Apply(CommentUpdatedEvent @event)
    {
        _id = @event.Id;
        _comments[@event.CommentId] = new Tuple<string, string>(@event.Comment, @event.Username);
    }

    public void RemoveComment(Guid commentId, string userName)
    {
        if (!_active)
        {
            throw new InvalidOperationException("You Can't Remove Comment Of Inactive Post!");
        }
        if (!_comments[commentId].Item2.Equals(userName, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new InvalidOperationException("You Aren't Allowed To Remove A Comment That Was Made By Another User!");
        }

        RaiseEvent(new CommentRemovedEvent
        {
            Id = _id,
            CommentId = commentId
        });
    }

    public void Apply(CommentRemovedEvent @event)
    {
        _id = @event.Id;
        _comments.Remove(@event.CommentId);
    }

    public void DeletePost(string userName)
    {
        if (!_active)
        {
            throw new InvalidOperationException("Post Has Already Been Removed!");
        }
        if (!_author.Equals(userName, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new InvalidOperationException("You Are Not Allowed to Remove A Post That Was Made by Someone Else!");
        }

        RaiseEvent(new PostRemovedEvent
        {
            Id = _id
        });
    }

    public void Apply(PostRemovedEvent @event)
    {
        _id = @event.Id;
        _active = false;
    }
}
