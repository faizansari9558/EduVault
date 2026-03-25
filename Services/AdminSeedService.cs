using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;

namespace SmartELibrary.Services;

public static class AdminSeedService
{
    public static async Task EnsureAdminExistsAsync(ApplicationDbContext dbContext, IMongoSequenceService sequenceService, string phoneNumber, string password, string fullName = "System Admin")
    {
        phoneNumber = (phoneNumber ?? string.Empty).Trim();
        password = password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var admin = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (admin is not null)
        {
            var existingAdmin = await dbContext.Admins.FirstOrDefaultAsync(x => x.UserId == admin.Id);
            if (existingAdmin is null)
            {
                // Id will be auto-assigned by SaveChangesAsync override in ApplicationDbContext
                dbContext.Admins.Add(new Admin { UserId = admin.Id, CreatedAtUtc = DateTime.UtcNow });
                await dbContext.SaveChangesAsync();
            }
            return;
        }

        var user = new User
        {
            // Id will be auto-assigned by SaveChangesAsync override in ApplicationDbContext
            FullName = fullName,
            PhoneNumber = phoneNumber,
            PasswordHash = PasswordService.HashPassword(password),
            Role = UserRole.Admin,
            IsApproved = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(); // user.Id is now assigned

        dbContext.Admins.Add(new Admin
        {
            UserId = user.Id,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
