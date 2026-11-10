namespace BookingApi.Errors;

/// <summary>Requested entity does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Request conflicts with current state, e.g. the slot is already taken. Mapped to HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Request is well-formed but breaks a business rule. Mapped to HTTP 422.</summary>
public class BusinessRuleException(string message) : Exception(message);
