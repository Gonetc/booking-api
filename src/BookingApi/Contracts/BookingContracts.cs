using System.ComponentModel.DataAnnotations;
using BookingApi.Domain;

namespace BookingApi.Contracts;

public record CreateBookingRequest(
    [Range(1, int.MaxValue)] int MasterId,
    [Range(1, int.MaxValue)] int ServiceId,
    DateOnly Date,
    TimeOnly StartTime,
    [Required, StringLength(100, MinimumLength = 2)] string ClientName,
    [Required, Phone, StringLength(30)] string ClientPhone);

public record BookingResponse(
    int Id,
    int MasterId,
    string MasterName,
    int ServiceId,
    string ServiceName,
    decimal Price,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ClientName,
    string ClientPhone,
    BookingStatus Status,
    DateTime CreatedAtUtc);
