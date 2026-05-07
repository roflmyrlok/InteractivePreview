using LocationService.Application.Commands;
using LocationService.Application.Interfaces;
using LocationService.Domain.Entities;
using MediatR;

namespace LocationService.Application.Handlers
{
    public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, UpdateLocationResult>
    {
        private readonly ILocationRepository _repo;

        public UpdateLocationCommandHandler(ILocationRepository repo)
        {
            _repo = repo;
        }

        public async Task<UpdateLocationResult> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
        {
            var location = await _repo.GetForUpdateAsync(request.Id);
            if (location == null)
                return new UpdateLocationResult { Outcome = UpdateOutcome.NotFound };

            if (location.RowVersion != request.RowVersion)
                return new UpdateLocationResult { Outcome = UpdateOutcome.Conflict };

            location.Address = request.Address;

            // Replace details collection: remove existing tracked details, add new ones.
            foreach (var existing in location.Details.ToList())
                _repo.RemoveDetail(existing);
            location.Details.Clear();

            foreach (var dto in request.Details)
            {
                location.Details.Add(new LocationDetail
                {
                    Id = Guid.NewGuid(),
                    LocationId = location.Id,
                    PropertyName = dto.PropertyName,
                    PropertyValue = dto.PropertyValue
                });
            }

            try
            {
                await _repo.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return new UpdateLocationResult { Outcome = UpdateOutcome.Conflict };
            }

            return new UpdateLocationResult
            {
                Outcome = UpdateOutcome.Updated,
                NewRowVersion = location.RowVersion
            };
        }
    }
}
