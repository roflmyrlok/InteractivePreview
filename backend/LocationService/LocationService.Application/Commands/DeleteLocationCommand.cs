using MediatR;

namespace LocationService.Application.Commands
{
    // Soft delete. The row is kept (IsDeleted=true, DeletedAt, DeletedByUserId);
    // queries filter it out via the global query filter.
    public class DeleteLocationCommand : IRequest<DeleteLocationResult>
    {
        public Guid Id { get; set; }
        public uint RowVersion { get; set; }
    }

    public enum DeleteOutcome
    {
        Deleted,
        NotFound,
        Conflict
    }

    public class DeleteLocationResult
    {
        public DeleteOutcome Outcome { get; set; }
    }
}
