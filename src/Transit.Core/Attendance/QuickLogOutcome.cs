namespace Transit.Core.Attendance;

// Result of a one-click log request from the reminder toast.
public enum QuickLogOutcome
{
    Logged,
    AlreadyLogged,
    StaleReminder,
    UnknownLocation,
}
