namespace Transit.Web.Endpoints;

// Body of a one-click log from the toast. Date is the day the toast was raised for.
public sealed record LogTodayRequest(DateOnly Date, int LocationId);
