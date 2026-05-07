using LocationService.Application.Commands;
using LocationService.Application.Interfaces;
using LocationService.Domain.Entities;
using MediatR;

namespace LocationService.Application.Handlers
{
    public class PatchLocationCommandHandler : IRequestHandler<PatchLocationCommand, UpdateLocationResult>
    {
        private readonly ILocationRepository _repo;

        public PatchLocationCommandHandler(ILocationRepository repo)
        {
            _repo = repo;
        }

        public async Task<UpdateLocationResult> Handle(PatchLocationCommand request, CancellationToken cancellationToken)
        {
            var location = await _repo.GetForUpdateAsync(request.Id);
            if (location == null)
                return new UpdateLocationResult { Outcome = UpdateOutcome.NotFound };

            if (location.RowVersion != request.RowVersion)
                return new UpdateLocationResult { Outcome = UpdateOutcome.Conflict };

            if (!string.IsNullOrEmpty(request.Address))
                location.Address = request.Address;

            // Upsert details by PropertyName. Empty PropertyValue == remove.
            foreach (var dto in request.Details)
            {
                var existing = location.Details.FirstOrDefault(d =>
                    string.Equals(d.PropertyName, dto.PropertyName, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(dto.PropertyValue))
                {
                    if (existing != null)
                    {
                        location.Details.Remove(existing);
                        _repo.RemoveDetail(existing);
                    }
                    continue;
                }

                if (existing != null)
                {
                    existing.PropertyValue = dto.PropertyValue;
                }
                else
                {
                    location.Details.Add(new LocationDetail
                    {
                        Id = Guid.NewGuid(),
                        LocationId = location.Id,
                        PropertyName = dto.PropertyName,
                        PropertyValue = dto.PropertyValue
                    });
                }
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
