using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using SmartELibrary.Models;
using SmartELibrary.Services;

namespace SmartELibrary.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMongoSequenceService sequenceService) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<DeletedUser> DeletedUsers => Set<DeletedUser>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialPage> MaterialPages => Set<MaterialPage>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<ProgressTracking> ProgressTrackings => Set<ProgressTracking>();
    public DbSet<MaterialPageProgress> MaterialPageProgress => Set<MaterialPageProgress>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<SemesterResultPublish> SemesterResultPublishes => Set<SemesterResultPublish>();

    // Collection name -> the entity type mapping used for sequence service
    private static readonly Dictionary<Type, string> _collectionNames = new()
    {
        { typeof(User), "users" },
        { typeof(DeletedUser), "deleted_users" },
        { typeof(Admin), "admins" },
        { typeof(Teacher), "teachers" },
        { typeof(Student), "students" },
        { typeof(Semester), "semesters" },
        { typeof(Subject), "subjects" },
        { typeof(Topic), "topics" },
        { typeof(TeacherSubject), "teacher_subjects" },
        { typeof(StudentEnrollment), "student_enrollments" },
        { typeof(Material), "materials" },
        { typeof(MaterialPage), "material_pages" },
        { typeof(Quiz), "quizzes" },
        { typeof(QuizQuestion), "quiz_questions" },
        { typeof(QuizResult), "quiz_results" },
        { typeof(ProgressTracking), "progress_trackings" },
        { typeof(MaterialPageProgress), "material_page_progress" },
        { typeof(OtpVerification), "otp_verifications" },
        { typeof(SemesterResultPublish), "semester_result_publishes" },
    };

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-assign IDs to any Added entity that still has Id == 0
        var newEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added)
            .ToList();

        foreach (var entry in newEntries)
        {
            var entityType = entry.Entity.GetType();
            var idProperty = entityType.GetProperty("Id");
            if (idProperty is null || idProperty.PropertyType != typeof(int)) continue;

            var currentId = (int)(idProperty.GetValue(entry.Entity) ?? 0);
            if (currentId != 0) continue; // already assigned

            if (_collectionNames.TryGetValue(entityType, out var collectionName))
            {
                var nextId = await sequenceService.GetNextIdAsync(collectionName, cancellationToken);
                idProperty.SetValue(entry.Entity, nextId);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToCollection("users");
        modelBuilder.Entity<DeletedUser>().ToCollection("deleted_users");
        modelBuilder.Entity<Admin>().ToCollection("admins");
        modelBuilder.Entity<Teacher>().ToCollection("teachers");
        modelBuilder.Entity<Student>().ToCollection("students");
        modelBuilder.Entity<Semester>().ToCollection("semesters");
        modelBuilder.Entity<Subject>().ToCollection("subjects");
        modelBuilder.Entity<Topic>().ToCollection("topics");
        modelBuilder.Entity<TeacherSubject>().ToCollection("teacher_subjects");
        modelBuilder.Entity<StudentEnrollment>().ToCollection("student_enrollments");
        modelBuilder.Entity<Material>().ToCollection("materials");
        modelBuilder.Entity<MaterialPage>().ToCollection("material_pages");
        modelBuilder.Entity<Quiz>().ToCollection("quizzes");
        modelBuilder.Entity<QuizQuestion>().ToCollection("quiz_questions");
        modelBuilder.Entity<QuizResult>().ToCollection("quiz_results");
        modelBuilder.Entity<ProgressTracking>().ToCollection("progress_trackings");
        modelBuilder.Entity<MaterialPageProgress>().ToCollection("material_page_progress");
        modelBuilder.Entity<OtpVerification>().ToCollection("otp_verifications");
        modelBuilder.Entity<SemesterResultPublish>().ToCollection("semester_result_publishes");

        // Explicitly define relationships to help MongoDB provider with Includes
        modelBuilder.Entity<StudentEnrollment>(entity =>
        {
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
            entity.HasOne(x => x.Semester).WithMany().HasForeignKey(x => x.SemesterId);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasOne(x => x.Semester).WithMany().HasForeignKey(x => x.SemesterId);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasOne(x => x.Semester).WithMany().HasForeignKey(x => x.SemesterId);
            entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
            entity.HasOne(x => x.Material).WithMany().HasForeignKey(x => x.MaterialId);
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasOne(x => x.Quiz).WithMany(x => x.QuizQuestions).HasForeignKey(x => x.QuizId);
        });

        modelBuilder.Entity<MaterialPage>(entity =>
        {
            entity.HasOne(x => x.Material).WithMany(x => x.Pages).HasForeignKey(x => x.MaterialId);
        });

        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.HasOne(x => x.Quiz).WithMany(x => x.QuizResults).HasForeignKey(x => x.QuizId);
        });
    }
}
