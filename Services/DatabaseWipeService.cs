using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;

namespace SmartELibrary.Services;

public static class DatabaseWipeService
{
    // Keeps auth/identity tables intact (Users/Admins/Teachers/Students) and preserves __EFMigrationsHistory.
    public static async Task WipeUserGeneratedContentAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // For MongoDB, we simply remove the ranges of items from the DbSets.
        dbContext.MaterialPageProgress.RemoveRange(dbContext.MaterialPageProgress);
        dbContext.QuizResults.RemoveRange(dbContext.QuizResults);
        dbContext.QuizQuestions.RemoveRange(dbContext.QuizQuestions);
        dbContext.Quizzes.RemoveRange(dbContext.Quizzes);
        dbContext.ProgressTrackings.RemoveRange(dbContext.ProgressTrackings);
        dbContext.MaterialPages.RemoveRange(dbContext.MaterialPages);
        dbContext.Materials.RemoveRange(dbContext.Materials);
        dbContext.StudentEnrollments.RemoveRange(dbContext.StudentEnrollments);
        dbContext.TeacherSubjects.RemoveRange(dbContext.TeacherSubjects);
        dbContext.OtpVerifications.RemoveRange(dbContext.OtpVerifications);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task WipeAllApplicationDataAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        dbContext.MaterialPageProgress.RemoveRange(dbContext.MaterialPageProgress);
        dbContext.QuizResults.RemoveRange(dbContext.QuizResults);
        dbContext.QuizQuestions.RemoveRange(dbContext.QuizQuestions);
        dbContext.Quizzes.RemoveRange(dbContext.Quizzes);
        dbContext.ProgressTrackings.RemoveRange(dbContext.ProgressTrackings);
        dbContext.StudentEnrollments.RemoveRange(dbContext.StudentEnrollments);
        dbContext.TeacherSubjects.RemoveRange(dbContext.TeacherSubjects);
        dbContext.MaterialPages.RemoveRange(dbContext.MaterialPages);
        dbContext.Materials.RemoveRange(dbContext.Materials);
        dbContext.Topics.RemoveRange(dbContext.Topics);
        dbContext.Subjects.RemoveRange(dbContext.Subjects);
        dbContext.Semesters.RemoveRange(dbContext.Semesters);
        dbContext.Students.RemoveRange(dbContext.Students);
        dbContext.Teachers.RemoveRange(dbContext.Teachers);
        dbContext.Admins.RemoveRange(dbContext.Admins);
        dbContext.OtpVerifications.RemoveRange(dbContext.OtpVerifications);
        dbContext.Users.RemoveRange(dbContext.Users);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
