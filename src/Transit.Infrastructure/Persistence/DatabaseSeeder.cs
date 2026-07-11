using Microsoft.EntityFrameworkCore;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    // Seeds Home + Office only when there are no locations yet.
    public static async Task SeedAsync(TransitDbContext db, CancellationToken ct = default)
    {
        if (await db.WorkLocations.AnyAsync(ct))
        {
            return;
        }

        db.WorkLocations.AddRange(
            new WorkLocation { Name = "Home",   ColorHex = "#4CAF50", CountsAsTravel = false, IsDefaultForUnmarked = true },
            new WorkLocation { Name = "Office", ColorHex = "#1976D2", CountsAsTravel = true,  IsDefaultForUnmarked = false });

        await db.SaveChangesAsync(ct);
    }
}