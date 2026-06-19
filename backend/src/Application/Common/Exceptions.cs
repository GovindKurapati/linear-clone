namespace LinearClone.Application.Common;

// Thrown when an entity expected to exist wasn't found.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Thrown when an optimistic-concurrency check fails — the row was modified by
// someone else between the client reading it and trying to save. The controller
// maps this to HTTP 409 Conflict. Defined here (not in Infrastructure) so the
// Api layer can catch it without referencing EF Core.
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }
}