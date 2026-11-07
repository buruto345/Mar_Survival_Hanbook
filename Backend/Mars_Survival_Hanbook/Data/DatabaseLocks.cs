using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data;

public static class DatabaseLocks
{
    // Transaction-scoped application locks serialize independent API/worker processes as well.
    public static Task AcquireAsync(MarsDbContext db, string key, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource={key}, @LockMode='Exclusive',
    @LockOwner='Transaction', @LockTimeout=15000;
IF @result < 0 THROW 51000, 'Could not acquire transaction lock', 1;", ct);
}
