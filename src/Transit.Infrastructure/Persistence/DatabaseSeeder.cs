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
            new WorkLocation { Name = "Home",   ColorHex = "#4C566A", CountsAsTravel = false, IsDefaultForUnmarked = true,  IsArchived = false, DisplayOrder = 0 },
            new WorkLocation { Name = "Office", ColorHex = "#3B5BDB", CountsAsTravel = true,  IsDefaultForUnmarked = false, IsArchived = false, DisplayOrder = 1 });

        await db.SaveChangesAsync(ct);
    }
}