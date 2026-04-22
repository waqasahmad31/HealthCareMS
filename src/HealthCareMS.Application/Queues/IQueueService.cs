using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Queues;

public interface IQueueService
{
    Task<Result<QueueEntryResponse>> RegisterWalkInAsync(WalkInRegistrationRequest request, CancellationToken cancellationToken);

    Task<Result<QueueEntryResponse>> CheckInAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<Result<QueueEntryResponse>> CallNextAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken);

    Task<Result<QueueEntryResponse>> GetNextPatientAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken);

    Task<Result<QueueBoardResponse>> GetBoardAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken);

    Task<Result<PatientQueueStatusResponse>> GetPatientStatusAsync(Guid appointmentId, CancellationToken cancellationToken);
}
