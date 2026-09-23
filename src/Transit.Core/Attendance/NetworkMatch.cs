using Transit.Core.Entities;

namespace Transit.Core.Attendance;

// A location suggested because the laptop is connected to its hinted network.
public sealed record NetworkMatch(WorkLocation Location, string Network);
