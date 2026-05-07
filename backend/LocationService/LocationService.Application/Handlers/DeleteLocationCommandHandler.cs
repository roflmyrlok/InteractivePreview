using LocationService.Application.Commands;
using LocationService.Application.Interfaces;
using MediatR;

namespace LocationService.Application.Handlers
{
    public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, DeleteLocationResult>
    {
        private readonly ILocationRepository _repo;
        private readonly ICurrentUserContext _currentUser;

        public DeleteLocationCommandHandler(ILocationRepository repo, ICurrentUserContext currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<DeleteLocationResult> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            var location = await _repo.GetForUpdateAsync(request.Id);
            if (location == null)
                return new DeleteLocationResult { Outcome = DeleteOutcome.NotFound };

            if (location.RowVersion != request.RowVersion)
                return new DeleteLocationResult { Outcome = DeleteOutcome.Conflict };

            location.IsDeleted = true;
            location.DeletedAt = DateTime.UtcNow;
            location.DeletedByUserId = _currentUser.UserId;

            try
            {
                await _repo.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return new DeleteLocationResult { Outcome = DeleteOutcome.Conflict };
            }

            return new DeleteLocationResult { Outcome = DeleteOutcome.Deleted };
        }
    }
}
