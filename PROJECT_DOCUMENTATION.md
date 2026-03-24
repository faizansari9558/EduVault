Version: 2.4
Last Updated: 23/03/2026
Updated By: Documentation Refresh

# EduVault (Smart E-Library)

## 1. Project Title
EduVault (Smart E-Library)

## 2. Project Summary
EduVault is an ASP.NET Core MVC web application for academic content delivery, quiz assessment, and progress analytics.

It supports three roles:
- Admin: governance, approvals, academic setup, reporting, and result publication
- Teacher: chapter creation, file upload, quiz creation, and analytics review
- Student: semester enrollment, learning, quiz attempts, and result viewing

## 3. Business Goals
- Create one centralized digital learning platform
- Keep semester and subject structure clear and controlled
- Track real learner behavior (time, page completion, quiz score)
- Give admin and teachers measurable analytics
- Publish semester results in a controlled workflow

## 4. Scope
### In Scope (Implemented)
- Role-based authentication and authorization (Admin, Teacher, Student)
- OTP flow for student login and account recovery
- Admin approval workflows for users and semester enrollments
- Semester, subject, and teacher assignment management
- Rich-text chapter materials with multi-page sequencing
- Standalone and chapter-linked quizzes
- Student progress tracking and weighted progress score
- Semester result computation and publish/unpublish control
- Student result viewing with print-friendly layout

### Out of Scope (Current Release)
- Payment gateway integration
- Live classes and video streaming
- Production SMS gateway integration (OTP provider abstraction is present)
- Multi-tenant institution management

## 5. Technology Stack
### Application Layer
- ASP.NET Core MVC (.NET 10, target framework net10.0)
- Razor Views + Bootstrap-based UI
- Session-based user state

### Data Layer
- MySQL database
- Entity Framework Core with Pomelo provider
- Code-first migrations

### Runtime and Tools
- dotnet CLI
- Visual Studio Code / Visual Studio
- XAMPP or managed MySQL (configurable)

## 6. High-Level Architecture
1. Browser sends request to MVC controllers.
2. Controllers validate role/session and request state.
3. Controllers call EF Core and domain services.
4. Data is read/written in MySQL.
5. Razor view is returned to the client.

Core implementation units:
- Controllers: role modules and user workflows
- Filters: RoleAuthorizeAttribute, student approval checks
- Services: OTP, progress calculation, result backfill/helper services
- Data: ApplicationDbContext with relational entities

## 7. Role-Wise Functional Modules
### Admin
- User management (approve, edit, delete with safety checks)
- Semester and subject management
- Teacher-subject assignment
- Enrollment request approval/rejection
- Semester-student grouped view
- Platform analytics and reports
- Semester result publication controls

### Teacher
- Upload chapter materials with at least 2 pages
- Upload file/link-based resources
- Create quizzes with optional availability and time limits
- Review student engagement and performance analytics

### Student
- OTP-based login and secure session handling
- Semester enrollment request submission
- Access approved learning resources
- Complete chapter pages and submit quizzes
- View published semester results and print output

## 8. Progress and Result Logic
### Material Progress Formula
Final Progress (%) =
- Screen Time % x 50%
- Quiz Score % x 40%
- Completion % x 10%

### Notes
- Page completion is captured once per page attempt lifecycle.
- Scroll depth may reduce effective reading-time contribution.
- Chapter-linked quizzes are used in per-material progress logic.
- Standalone quizzes are tracked separately for quiz reporting.

### Semester Result Logic
- Chapter Result: from stored material progress percent
- Subject Average: average of chapter results within a subject
- Semester Final Result: average of subject averages

## 9. Data Model Snapshot
Primary entities:
- Users, Admins, Teachers, Students
- Semesters, Subjects, Topics
- TeacherSubjects
- StudentEnrollments
- Materials, MaterialPages, MaterialPageProgress
- Quizzes, QuizQuestions, QuizResults
- ProgressTrackings
- OtpVerifications
- DeletedUsers
- SemesterResultPublishes

## 10. Security and Compliance Notes
- Passwords are hashed (not plaintext)
- Server-side role guard on secured actions
- Session-based authentication flow
- OTP validity and one-time verification controls
- Validation on all key input workflows

## 11. Environment and Configuration
Configuration can be supplied via:
- appsettings.json
- appsettings.Development.json
- environment variables
- command-line arguments

Common database configuration keys:
- ConnectionStrings:DefaultConnection
- Database:Host
- Database:Port
- Database:Name
- Database:User
- Database:Password

## 12. Local Run Guide
1. Ensure MySQL server is running.
2. Ensure schema exists (example: smartelibrary_db).
3. Start the app:
   - dotnet run
   - optional DB override via command-line arguments
4. Open localhost URL from runtime output.

## 13. Quality Checklist
- Build passes with no blocking errors
- Role guards validated on protected routes
- Enrollment and approval workflows verified
- Progress aggregation outputs expected values
- Published/unpublished result visibility validated
- Mobile responsiveness validated on shared header and critical pages

## 14. Known Limitations
- SMS delivery is environment-dependent
- High-volume analytics may need future query optimization
- UI currently prioritizes clarity over complex interaction patterns

## 15. Change Log
- 2.4: Documentation refresh, terminology alignment to EduVault, feature and flow consistency updates
- 2.3: Semester result publication and analytics details expanded
