Version: 2.3
Last Updated: 14/03/2026
Updated By: Auto-Documentation System

# Smart E-Library

## 1. Project Title
Smart E-Library

## 2. Project Overview (simple explanation)
Smart E-Library is a web application for managing study materials (chapters/notes), quizzes, and student learning progress.

It helps:
- Admins manage semesters, subjects, teachers, and students
- Teachers upload learning content and quizzes
- Students read content, take quizzes, and track their progress

## 3. Project Definition
Smart E-Library is an online learning library and progress-tracking system.

It stores educational content by semester/subject/topic and records:
- Which pages a student visited
- How much time they spent reading
- Quiz scores
- Overall learning progress analytics

## 4. Project Objectives
- Provide a central place for learning materials
- Support structured learning by semester → subject → topic
- Allow teachers to create chapters with multiple pages and optional quizzes
- Track student progress in a measurable way (time, completion, quiz score)
- Provide analytics dashboards for teachers and administrators

## 5. Project Scope
**In scope (currently implemented):**
- User management with three roles: Admin, Teacher, Student (role details stored in separate role tables)
- Login flows:
  - Admin login with password
  - Teacher login with password
  - Student login using OTP (one-time password)
- Admin:
  - Manage users (approve/deny)
  - Create semesters and subjects
  - Assign teachers to subjects
  - Review and decide semester enrollment requests
  - View system-wide progress reports
  - Generate and publish semester results (per-student and per-semester)
  - Control result visibility (publish / unpublish per semester)
- Teacher:
  - Upload chapter-style materials as rich-text pages
  - Optionally attach a quiz after a page (multiple questions supported)
  - Create quizzes
  - View student progress analytics for the teacher’s own materials
- Student:
  - Enroll in a semester (requires admin approval)
  - Browse the library and read materials after approval
  - Read chapter pages in order (with completion tracking)
  - Attempt quizzes after approval
  - View semester results (only after Admin publishes them)
  - Print or download result as PDF via browser print dialog

**Out of scope (not implemented / not guaranteed):**
- Payment features
- Live classes/video streaming
- Real SMS gateway integration (OTP SMS is a placeholder)
- Advanced admin reporting filters (beyond the current dashboard)

## 6. Technologies Used
### Frontend
- ASP.NET Core MVC Razor Views (server-rendered UI)
- HTML + Bootstrap-style UI (based on existing views)
- JavaScript (for basic interactivity where needed)

### Backend
- .NET 10 (ASP.NET Core)
- ASP.NET Core MVC Controllers
- Session-based authentication/authorization (custom role filter)
- Service layer for OTP and progress calculations

### Database
- MySQL (via `Pomelo.EntityFrameworkCore.MySql`)
- Entity Framework Core (EF Core) for ORM and migrations

### Tools & Platforms
- .NET SDK (project targets `net10.0`)
- Entity Framework Core Migrations
- Visual Studio Code / Visual Studio (typical development tools)

## 7. System Architecture Overview (explained in text form)
The system uses a simple “web app + database” architecture:

1) The user opens the web application in a browser.
2) The browser sends requests to ASP.NET Core Controllers (backend).
3) Controllers:
   - Check the user session (who is logged in and which role)
   - Load or save data using EF Core
   - Return a Razor View (HTML page) to the browser
4) The database (MySQL) stores all permanent records like users, semesters, materials, quiz questions, quiz results, and progress.

Key internal parts:
- **Controllers**: handle pages like Admin Dashboard, Teacher Upload Material, Student Library, etc.
- **Filters**: `RoleAuthorizeAttribute` blocks pages for users who are not allowed.
- **Services**:
  - OTP generation/verification
  - Progress score calculation
- **EF Core DbContext** (`ApplicationDbContext`): defines all tables (entities).

## 8. Database Details
### Database Name
Default database name (configurable): `smartelibrary_db`

Where it is defined:
- In configuration under `Database:Name` (and related host/port/user/password)
- Or via `ConnectionStrings:DefaultConnection` if provided

### List of Tables
Based on the current EF Core model, the database tables are:
- Admins
- Teachers
- Students
- Users
- Semesters
- Subjects
- Topics
- TeacherSubjects
- StudentEnrollments
- Materials
- MaterialPages
- MaterialPageProgress
- Quizzes
- QuizQuestions
- QuizResults
- ProgressTrackings
- OtpVerifications
- SemesterResultPublishes

### Table Description (simple)
- **Users**: Stores shared identity and login fields used for authentication and authorization (phone, password hash, role, approval status, created date, etc.).
- **Admins/Teachers/Students**: Store role-specific fields and link back to Users (one-to-one).
- **Admins**: Stores admin-specific details and links to Users (one-to-one).
- **Teachers**: Stores teacher-specific details and links to Users (one-to-one). Includes **Teacher ID** (column: `TeacherId`, format: `TID0001`).
- **Students**: Stores student-specific details and links to Users (one-to-one). Includes Enrollment Number.
- **Semesters**: Stores semester list (e.g., “Semester 1”), and whether it is active.
- **Subjects**: Subjects belong to a semester (e.g., “Math” in “Semester 1”).
- **Topics**: Topics belong to a subject (optional grouping such as “Algebra”).
- **TeacherSubjects**: Mapping table that assigns teachers to subjects (who can teach/upload for what).
- **StudentEnrollments**: Records which student is enrolled in which semester, including admin approval fields.
- **Materials**: Learning materials uploaded/created by teachers. Each material belongs to a semester and subject (and optional topic). Some materials can be public.
- **MaterialPages**: A material can have multiple pages (rich-text/HTML content) with page numbers.
- **MaterialPageProgress**: Tracks page-level reading progress for each student (time spent, started/completed times, completed flag).
- **Quizzes**: Quizzes created by teachers. A quiz belongs to a subject and teacher; it may also be linked to a material and/or a specific material page.
- **QuizQuestions**: Stores questions and options for a quiz.
- **QuizResults**: Stores a student’s quiz attempt score and submission time.
- **ProgressTrackings**: Stores overall progress values (screen time, completion, quiz score, progress percent, low engagement flag) for a student per subject/topic/material.
- **OtpVerifications**: Stores OTP codes and validity status for phone verification.
- **SemesterResultPublishes**: Tracks whether a semester's results have been published by an admin. One row per semester. Stores `IsPublished`, `PublishedAtUtc`, and `PublishedByAdminId`.

### Relationships
In simple terms:
- One **Semester** has many **Subjects**.
- One **Subject** has many **Topics**.
- One **Teacher (User)** can upload many **Materials**.
- One **Material** has many **MaterialPages**.
- One **MaterialPage** can have many **MaterialPageProgress** records (one per student).
- One **Quiz** has many **QuizQuestions**.
- One **Quiz** has many **QuizResults** (one per student attempt).
- **TeacherSubjects** connects Teachers ↔ Subjects (many-to-many).
- **StudentEnrollments** connects Students ↔ Semesters (many-to-many) and includes admin approval.
- **Admins**, **Teachers**, and **Students** each link one-to-one to **Users**.

Approval and tracking highlights:
- **StudentEnrollments** includes: `IsApproved`, `ApprovedAt`, `ApprovedByAdminId`.
- **MaterialPageProgress** stores reading time and completion.
- **SemesterResultPublishes** has a unique index on `SemesterId` (one publish record per semester).

### ER explanation in simple language
Think of the database like linked lists of information:
- First you create semesters.
- Inside each semester you add subjects.
- Teachers are assigned to subjects.
- Teachers create materials and pages for those subjects.
- Students enroll in semesters.
- Students read pages and take quizzes.
- The system stores progress for each student so teachers/admins can see analytics.

## 9. Functional Requirements
### Authentication & Access
- Users register via separate Teacher and Student registration pages.
- Phone verification is done using OTP.
- Teachers and students must be approved by Admin before they can access their dashboards.
- Each panel is role-protected:
  - Admin pages for Admin only
  - Teacher pages for Teacher only
  - Student pages for Student only

### Admin Features
- View dashboard summary (total users, students, teachers, materials, quizzes).
- Approve users.
- Edit or delete users (with rules to prevent deleting system admin or users with dependencies).
- View detailed user list with teacher IDs and student enrollment details.
- Create and manage semesters.
- Create and manage subjects.
- Assign teachers to subjects.
- Approve or reject semester enrollment requests.
- View students grouped by semester.
- View system progress analytics report.
- **Semester Result Management**:
  - View all semesters with publication status and enrolled student count.
  - Drill into a semester to see all approved students' computed results (sorted by result %).
  - View full per-subject, per-chapter result breakdown for any individual student.
  - Publish results for a semester → students can then view their own results.
  - Unpublish results at any time → students lose access until re-published.

### Teacher Features
- View dashboard summary (material count, quiz count, low engagement count).
- Upload chapter materials with:
  - Title and description
  - Multiple pages (page title + rich text content)
   - Minimum 2 pages per chapter
   - Page length guidance (encourages splitting long content into separate pages)
  - Optional quiz after a page (multiple questions supported; quiz title optional)
- Insert or delete pages before saving, with automatic page re-sequencing
- View uploaded materials.
- Preview chapter pages.
- Create standalone quizzes with optional time limit and availability window.
- View “Student Progress Analytics” for students who interacted with the teacher’s materials.
- Each teacher is assigned a unique Teacher ID (e.g., `TID0001`).

### Student Features
- Login using OTP.
- Enroll in semesters and wait for approval.
- View dashboard (recent progress items and averages).
- Browse library materials by semester/subject and search.
- Read materials:
  - Chapters can be read page-by-page
   - Opening a page creates a reading-progress record if one does not already exist
   - First-time completion stores active reading time, scroll depth, and completion status
   - Re-visits do not overwrite already completed page progress
- Quizzes:
   - Quizzes can be linked to chapter pages or created as standalone subject quizzes
   - Page quizzes appear in a modal popup only when the completed page has a quiz with questions and the student has not already attempted it
   - Standalone quizzes can have optional opening/closing times and an optional time limit
   - Quiz scores are stored for analytics; there is no pass/fail threshold in the current code
- **Semester Results**:
   - Student can view a list of their enrolled semesters that have published results
   - For each published semester, student sees a subject-wise and chapter-wise breakdown
   - Each chapter row shows: Completion %, Quiz Score %, and Chapter Result %
   - Each subject shows a Subject Average (average of chapter results in that subject)
   - Semester Final Result = average of all Subject Averages
   - A print/download option opens a clean print-layout page that auto-triggers the browser print dialog (student can Save as PDF)

### Progress Analytics Formula (Current)
Student final progress is calculated using weighted metrics:
- Screen Time % → 50%
- Quiz Score % → 40%
- Completion % → 10%

Formula:
- Final Progress (%) = (Screen Time % × 0.50) + (Quiz Score % × 0.40) + (Completion % × 0.10)

### Progress Calculation Criteria (Actual Code Rules)
The current code calculates per-material student progress using the following rules.

#### A) Upload Chapter Criteria
- Teacher must select a subject that is already assigned to that teacher.
- Semester is auto-derived from the selected subject.
- Chapter title is required.
- A chapter must contain at least 2 pages.
- Every page must have:
  - Page title
  - Rich-text HTML content
- Page content must stay within the rich-text length validator limit.
- A page quiz is optional.
- If a page quiz is added, every question must contain:
  - Question text
  - Option A
  - Option B
  - Option C
  - Option D
  - Correct option limited to `A`, `B`, `C`, or `D`
- If a page quiz title is left blank, the system auto-generates one in the format `Quiz - {Material Title} - Page {PageNumber}`.
- Saved chapter pages are numbered sequentially and stored as rich-text chapter material.

#### B) Standalone Create Quiz Criteria
- Teacher must select a valid subject assigned to that teacher.
- Quiz title is required and limited to 180 characters.
- The quiz must contain at least 1 question.
- Each question must contain:
   - Question text (up to 500 characters)
   - Option A, B, C, and D (each up to 200 characters)
   - Correct option limited to `A`, `B`, `C`, or `D`
- Time limit is optional, but when used it must be between 1 and 300 minutes.
- `Available From` and `Available To` are optional; when entered, they are converted from local time to UTC before storage.
- Standalone quizzes are stored without a linked chapter page (`MaterialPageId = null`).

#### C) Student Progress Calculation Criteria
- Progress is recalculated when:
   - A student completes a chapter page for the first time
   - A student submits a chapter-linked quiz for the first time
- Re-opening or re-completing an already completed page does not overwrite stored page time.
- Re-submitting an already submitted page quiz does not create a second score for progress calculation.
- Only chapter-linked quizzes are included in per-material progress calculation.
- Standalone quizzes created from `Create Quiz` are stored and reported separately; they do not feed the per-material progress formula.

#### D) Screen Time % Criteria
- Total recorded reading time is the sum of page `TimeSpentSeconds` for the student on that material.
- For the formula, the system uses an effective reading time that can be reduced by low scroll depth.
- If a page scroll depth is below 30%, that page's time contribution is scaled by `scrollDepth / 30`.
- If scroll depth is 30% or above, the full page time counts.
- Ideal reading reference is fixed at 6 minutes per page.
- Ideal total reading time = `Total Pages × 6 minutes`.
- Screen Time % = `(Effective Reading Time / Ideal Total Reading Time) × 100`, capped at 100%.

#### E) Quiz Score % Criteria
- The system collects all chapter-linked quizzes for the material.
- For each quiz, only the student's latest attempt is used.
- Quiz score is based on total correct answers across the material's chapter-linked quizzes.
- Quiz Score % = `(Total Correct Answers / Total Quiz Questions) × 100`.
- If quizzes exist but the student has not attempted them, Quiz Score % remains 0.
- If no chapter-linked quiz exists for the material, Quiz Score % is 0.

#### F) Completion % Criteria
- Completion % = `(Completed Pages / Total Pages) × 100`.
- A page counts as completed when the student posts `CompletePage` for that page.

#### G) Final Progress Output Stored in Database
- `ScreenTimeSeconds`
- `ScrollDepthPercent` (average scroll depth)
- `CompletionPercent`
- `QuizScorePercent`
- `QuizCorrectAnswers`
- `QuizTotalQuestions`
- `ProgressPercent`
- `LastUpdatedUtc`
- `IsLowEngagementAlert`

#### H) Low Engagement Criteria
- A page is marked low-engagement when first-time completion records less than 30 active seconds.
- Material progress is also marked low-engagement when the minimum completed-page time for the material is below 30 seconds.
- Low-engagement status affects teacher/admin analytics and alerts; it does not block student navigation.

#### I) Student-Facing Progress Status
- `Not Started`: no page-progress record and no quiz activity for the material
- `In Progress`: some activity exists, but completion is below 100%
- `Completed`: completion is 100%

Status bands used in analytics:
- Skimmer: < 20%
- NeedsImprovement: ≥ 20% and < 40%
- Learning: ≥ 40% and < 60%
- Progressing: ≥ 60% and < 80%
- ActiveLearner: ≥ 80% and < 90%
- Mastered: ≥ 90%

#### J) Semester Result Calculation
The semester result system aggregates per-material progress into a single semester result score.

**Step 1 — Chapter Result %**
- For each chapter (rich-text material with at least one page), the stored `ProgressPercent` in `ProgressTrackings` is used directly.
- This value is maintained automatically by the system every time a student completes a page or submits a quiz.

**Step 2 — Subject Average %**
- For each subject in the semester, all chapters belonging to that subject are collected.
- Subject Average = average of all chapter `ProgressPercent` values for that student.
- Chapters with no `ProgressTracking` record count as 0%.

**Step 3 — Semester Final Result %**
- Semester Final Result = average of all Subject Averages for the semester.
- Subjects with no chapters are excluded from the average.

**Result Status Bands (Semester Result)**
| Range | Status |
|---|---|
| ≥ 80% | Excellent |
| ≥ 60% and < 80% | Good |
| ≥ 40% and < 60% | Average |
| < 40% | Needs Improvement |

**Publish Control**
- Results are only visible to students after an Admin explicitly publishes them for that semester.
- The admin can unpublish at any time to hide results.
- Result calculation happens live at request time (not cached), so the result always reflects the latest progress data.

## 10. Non-Functional Requirements
### Performance
- Pages should load quickly for normal classroom-sized usage.
- Progress analytics should return results without long delays.
- Database queries should be optimized for common dashboards.

### Security
- Passwords are stored as hashes (not plain text).
- Role-based access control is enforced using a server-side filter.
- Sessions are used to store login state.
- OTP expires (time-limited) and can only be used once.

### Usability
- UI should be simple and readable for teachers and students.
- Workflows should require minimal steps (login → dashboard → action).
- Clear messages should be shown for invalid login/OTP.

### Scalability
- The system should be able to scale by:
  - Running multiple web servers (with a shared session strategy if needed)
  - Using a stronger database server
  - Adding caching for heavy reports

### Reliability
- Data consistency is enforced with database relationships and unique indexes.
- Critical operations (OTP verification, quiz submissions, page completion) must store data reliably.

## 11. User Roles & Permissions
### Admin
Can:
- Approve and manage users
- Create semesters and subjects
- Assign teachers to subjects
- View system-wide reports
- Generate, view, publish, and unpublish semester results

### Teacher
Can:
- Manage their own materials and quizzes
- View progress analytics for students who used their materials

Cannot:
- Access admin management functions

### Student
Can:
- Enroll in semesters
- Access library materials (based on enrollment and public materials)
- Read pages and attempt quizzes
- View published semester results (subject-wise and chapter-wise breakdown)
- Print or download semester result as PDF

Cannot:
- Upload materials
- Manage other users
- View results before Admin publishes them

## 12. Application Workflow (Step-by-step process)
### A) Teacher Registration
1. Teacher opens the Teacher Registration page.
2. Teacher enters name, phone number, and password.
3. System creates the account and Teacher record (with Teacher ID like `TID0001`).
4. System generates OTP.
5. Teacher verifies OTP.
6. Teacher waits for Admin approval.

### B) Student Registration
1. Student opens the Student Registration page.
2. Student enters name, enrollment number, phone number, and password.
3. System creates the account and Student record.
4. System generates OTP.
5. Student verifies OTP.
6. Student waits for Admin approval.

### C) Admin Workflow
1. Admin logs in.
2. Admin opens Users page.
3. Admin approves teachers/students.
4. Admin creates semesters and subjects.
5. Admin assigns teachers to subjects.
6. Admin reviews semester enrollment requests and approves/rejects.
7. Admin can view students grouped semester-wise.
8. Admin views system reports.
9. Admin opens **Semester Results**.
10. Admin selects a semester to view all enrolled students' computed results.
11. Admin can click any student to view full per-chapter result breakdown.
12. Admin clicks **Publish Results** when ready → students can now view their results.
13. Admin can **Unpublish** at any time to hide results from students.

### D) Teacher Workflow
1. Teacher logs in.
2. Teacher opens Upload Material.
3. Teacher selects assigned subject (semester auto-fills).
4. Teacher enters chapter title and optional description.
5. Teacher adds at least 2 pages.
6. Each page must contain a page title and rich-text content.
7. Teacher can optionally add a quiz after any page.
8. If a page quiz is added, each question must include text, options A-D, and one correct option.
9. Teacher saves the chapter.
10. Teacher can also create standalone quizzes with optional time limit and availability window.
11. Teacher views student progress analytics.

### E) Student Workflow
1. Student logs in with phone number → OTP.
2. Student enrolls in a semester.
3. Student waits for admin approval.
4. Student opens Library after approval.
5. Student selects a material and reads it.
6. Opening a page starts or resumes a page-progress record.
7. When the student completes a page, the system records active time, scroll depth, and completion status.
8. If the completed page has a quiz and the student has not already attempted it, the quiz appears in a modal popup.
9. The system stores quiz score for analytics and recalculates material progress.
10. Student views the dashboard and progress updates.
11. When Admin publishes results, student opens **My Results**.
12. Student selects their semester to view subject-wise and chapter-wise result breakdown.
13. Student clicks **Print / Download PDF** to print or save the result as a PDF.

### F) Semester Result Workflow
```
[Admin] Semester Results page
         │
         ▼
   List of semesters → click "View Results" on a semester
         │
         ▼
   See all enrolled students with computed Final Result %
         │
         ├── Click a student → Full per-chapter breakdown (Admin Student Result)
         │
         ├── [NOT PUBLISHED] → Click "Publish Results"
         │                    → Students can now see their result
         │
         └── [PUBLISHED]     → Click "Unpublish Results"
                              → Result hidden from students again

[Student] My Results page
         │
         ▼
   List of semesters with published results
         │
         ▼
   Click a semester → Subject + Chapter breakdown + Final Result %
         │
         ▼
   Click "Print / Download PDF"
         │
         ▼
   Print layout opens in new tab → auto-triggers print dialog
   Student chooses "Save as PDF"
```

## 13. Deployment Details
### Local Development
- Default HTTP URL: `http://localhost:5079`
- Launch profiles are defined in `Properties/launchSettings.json`.

Steps:
1. Configure MySQL connection details in appsettings (host/port/name/user/password), or set `ConnectionStrings:DefaultConnection`.
2. Ensure MySQL server is running and accessible.
3. Run the application using `dotnet run`.

### Database Setup
- The application applies schema changes using EF Core migrations (`Database.Migrate()`), not `EnsureCreated`.
- Migrations keep data safe and allow controlled updates.
- On startup the system can also run a temporary backfill to ensure role tables (Admins/Teachers/Students) match existing Users.

### Production Notes (high level)
- Store database credentials securely (not in source code).
- Use HTTPS and a reverse proxy (IIS/Nginx/Apache) if needed.
- Consider a distributed session approach if running multiple servers.

## 14. Limitations
- OTP SMS sending is a placeholder (no real SMS gateway integration).
- Session-based authentication may need redesign for large-scale deployments.
- Reporting is currently focused on progress analytics and may not cover all business reports.
- Material content storage uses rich-text HTML for chapter pages; file uploads depend on implementation details and server storage.
- Semester result calculation only includes rich-text chapter materials (MaterialType = Notes) that have at least one page. PDF/PPT/Image/ExternalLink materials are excluded from the result formula.
- No PDF library is installed; PDF download relies on the browser's built-in "Save as PDF" print feature.
- Semester results are calculated live at request time (not pre-computed), which is fine for normal classroom-sized usage but may be slow for very large datasets.

## 15. Future Enhancements
- Integrate a real SMS provider for OTP delivery.
- Add email notifications for approval and quiz completion.
- Add advanced filters for reports (by semester/subject/topic/date range).
- Add downloadable report export (PDF/Excel).
- Add better audit logs (who changed what and when).
- Improve scalability with caching and background jobs for heavy analytics.

## 16. Version History
| Version | Date | Changes |
|---------|------|---------|
| 2.3 | 14/03/2026 | Added Semester Result Management System documentation: admin publish/unpublish workflow, student result view and PDF download, semester result calculation formula (chapter → subject average → semester average), result status bands, new SemesterResultPublishes DB table, and updated scope, features, workflow, limitations, and version history |
| 2.2 | 14/03/2026 | Corrected student progress documentation to match current code, added exact chapter upload and standalone quiz validation criteria, and documented the real progress calculation rules including scroll-depth weighting and low-engagement logic |
| 2.1 | 26/02/2026 | Updated student progress analytics formula to 50/40/10 (Screen Time/Quiz/Completion) and documented active status bands (Skimmer → Mastered) |
| 2.0 | 22/02/2026 | Major architecture and workflow improvements (normalized role tables, separate registration, admin approvals, semester enrollment approvals, page-based content, enhanced quizzes, engagement tracking, Teacher ID rename) |
| 1.0 | 21/02/2026 | Initial documentation created based on current project state |
| 1.1 | 21/02/2026 | Added Admin/Teacher/Student role tables, Teacher Code, and enrollment approval fields |
| 1.2 | 21/02/2026 | Split registration into Teacher and Student flows and added dedicated pages |
| 1.3 | 21/02/2026 | Added semester enrollment approval and restricted library access until approval |
| 1.4 | 21/02/2026 | Enhanced admin users view and added semester-wise student grouping page |
| 1.5 | 21/02/2026 | Enforced page-based content rules and soft length validation for teacher uploads |
| 1.6 | 21/02/2026 | Added page insertion/deletion controls and edit support for rich-text chapters |
| 1.7 | 21/02/2026 | Added page-level multi-question quizzes with modal student experience |
| 1.8 | 21/02/2026 | Enforced minimum reading time before completing a page |

## 17. Activity Flow & Workflow Diagrams

### A) Teacher Registration & Login Flow
```
┌─────────────────────────┐
│  Teacher Registration   │
│  Page                   │
└────────────┬────────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Enter: Name,         │
  │ Phone, Password      │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Create User &        │
  │ Teacher Record       │
  │ (TeacherId = TID###) │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Generate & Send OTP  │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Enter OTP & Verify   │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Status: Pending      │
  │ Approval             │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Admin Approves User  │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Teacher Login        │
  │ (Phone + Password)   │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Teacher Dashboard    │
  └──────────────────────┘
```

### B) Student Registration & Enrollment Flow
```
┌─────────────────────────┐
│  Student Registration   │
│  Page                   │
└────────────┬────────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Enter: Name,         │
  │ Enrollment No,       │
  │ Phone, Password      │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Create User &        │
  │ Student Record       │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Generate & Send OTP  │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Enter OTP & Verify   │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Status: Pending      │
  │ Approval             │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Admin Approves User  │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Student Login        │
  │ (Phone + OTP)        │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Enroll in Semester   │
  │ (Request Created)    │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Admin Reviews        │
   │ & Approves Enrollment│
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Student can Access   │
  │ Library Materials    │
  └──────────────────────┘
```

### C) Teacher Material Upload & Quiz Flow
```
┌─────────────────────────┐
│  Teacher Dashboard      │
│  "Upload Material"      │
└────────────┬────────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Select Semester &    │
  │ Subject              │
  │ (Topic optional)     │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Add Material Title & │
  │ Description          │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Add Page(s):         │
  │ - Title              │
  │ - Rich-text Content  │
   │ (Minimum 2 pages)    │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ For Each Page:       │
  │ Attach Optional Quiz?│
  └──────────┬───────────┘
             │
      ┌──────┴──────┐
      │             │
      ▼             ▼
   [YES]          [NO]
      │             │
      │    ┌────────┘
      │    │
      ▼    ▼
  ┌──────────────────────┐
  │ If Quiz Selected:    │
  │ - Enter Q Title      │
  │ - Add Questions      │
   │ - Set Correct Option │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Review & Save        │
  │ Material             │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Material Stored in   │
  │ Database             │
  └──────────────────────┘
```

### D) Student Reading & Progress Tracking Flow
```
┌─────────────────────────┐
│  Student Dashboard      │
│  "Browse Library"       │
└────────────┬────────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Filter by Semester/  │
  │ Subject/Topic        │
  │ (Search optional)    │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Select Material to   │
  │ Read                 │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ View Material Info & │
  │ Start Reading        │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Read Page 1 Content  │
  │ Progress Starts      │
  │ Track Activity       │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Student Completes    │
  │ Page                 │
  └──────────┬───────────┘
             │
  ┌──────────────────────┐
  │ Mark Page Complete   │
  │ Record: Active Time, │
  │ Scroll, Status       │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Quiz Attached?       │
  └──────────┬───────────┘
             │
      ┌──────┴──────┐
      │             │
      ▼             ▼
   [YES]          [NO]
      │             │
      │    ┌────────┘
      │    │
      ▼    ▼
  ┌──────────────────────┐
  │ Show Quiz Modal      │
  │ Answer Questions     │
  │ Submit              │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
   │ Calculate Score &    │
   │ Save Attempt         │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ Update Student       │
  │ Progress Record      │
  │ (Score, Time, etc)   │
  └──────────┬───────────┘
             │
             ▼
  ┌──────────────────────┐
  │ More Pages?          │
  └──────────┬───────────┘
             │
      ┌──────┴──────┐
      │             │
      ▼             ▼
   [YES]          [NO]
      │             │
      │    ┌────────┘
      │    │
      ├────┘
      │
      ▼
  ┌──────────────────────┐
  │ Go to Next Page      │
  │ (Repeat Process)    │
  └──────────────────────┘
```

### E) Admin Approval & Management Flow
```
┌─────────────────────────┐
│  Admin Dashboard        │
└────────────┬────────────┘
             │
      ┌──────┼──────┬──────────┬──────────┐
      │      │      │          │          │
      ▼      ▼      ▼          ▼          ▼
  ┌─────┐┌──────┐┌──────┐┌────────┐┌──────────┐
  │User ││Users ││Semes-││Enroll  ││Progress  │
  │Mgmt ││      ││ters  ││Approval││Analytics │
  └──┬──┘└──┬───┘└──┬───┘└───┬────┘└────┬─────┘
     │      │       │        │          │
     ▼      ▼       ▼        ▼          ▼
  ┌──────────────┐ ┌─────────────────┐ ┌──────────┐
  │Approve/Deny  │ │Create/Manage    │ │View & act│
  │Teachers &    │ │Semesters &      │ │on pending│
  │Students      │ │Subjects         │ │requests  │
  └──────────────┘ └─────────────────┘ └──────────┘
     │
     ▼
  ┌──────────────────────┐
  │User Status Changed   │
  │Record Updated        │
  └──────────────────────┘
```

## 18. Entity Relationship (ER) Diagram

### Central User Entity & Relationships
```
                            ┌──────────────────┐
                            │      USERS       │
                            ├──────────────────┤
                            │ Id (PK)          │
                            │ FullName         │
                            │ PhoneNumber (UQ) │
                            │ PasswordHash     │
                            │ Role             │
                            │ IsApproved       │
                            │ IsPhoneVerified  │
                            │ EnrollmentNo     │
                            │ CreatedAtUtc     │
                            └────────┬─────────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
                    ▼                ▼                ▼
            ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
            │   ADMINS     │  │  TEACHERS    │  │  STUDENTS    │
            ├──────────────┤  ├──────────────┤  ├──────────────┤
            │ Id (PK)      │  │ Id (PK)      │  │ Id (PK)      │
            │ UserId (FK,UQ)  │ UserId (FK,UQ)  │ UserId (FK,UQ)  │
            │ CreatedAtUtc │  │ TeacherId(UQ)   │ SemesterLimit  │
            └──────────────┘  └────┬─────────┘  └────┬──────────┘
                                   │                 │
                    ┌──────────────┘                 │
                    │                                │
                    ▼                                ▼
            ┌──────────────────┐         ┌──────────────────────┐
            │ TEACHERSUBJECTS  │         │STUDENTENROLLMENTS    │
            ├──────────────────┤         ├──────────────────────┤
            │ Id (PK)          │         │ Id (PK)              │
            │ TeacherId (FK)   │         │ StudentId (FK)       │
            │ SubjectId (FK)   │         │ SemesterId (FK)      │
            │ (Unique)         │         │ IsApproved           │
            └──────────────────┘         │ ApprovedByAdminId(FK)│
                    │                    │ ApprovedAt           │
                    │                    │ EnrollmentDate       │
                    │                    │ CreatedAtUtc         │
                    │                    └──────────────────────┘
                    │
                    ▼
            ┌──────────────────┐
            │    SUBJECTS      │
            ├──────────────────┤
            │ Id (PK)          │
            │ Name             │
            │ SemesterId (FK)  │
            │ CreatedAtUtc     │
            └────────┬─────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
   ┌────────┐  ┌─────────┐  ┌──────────────┐
   │ TOPICS │  │MATERIALS│  │   QUIZZES    │
   └────────┘  └────┬────┘  └────┬─────────┘
                    │             │
                    ▼             ▼
           ┌──────────────────┐ ┌──────────────┐
           │ MATERIALPAGES    │ │QUIZQUESTIONS │
           ├──────────────────┤ ├──────────────┤
           │ Id (PK)          │ │ Id (PK)      │
           │ MaterialId (FK)  │ │ QuizId (FK)  │
           │ PageNumber       │ │ Question     │
           │ Title            │ │ Options      │
           │ Content (HTML)   │ └──────────────┘
           │ CreatedAtUtc     │
           └────────┬─────────┘
                    │
                    ▼
        ┌────────────────────────────┐
        │ MATERIALPAGE PROGRESS      │
        ├────────────────────────────┤
        │ Id (PK)                    │
        │ StudentId (FK)             │
        │ MaterialPageId (FK)        │
        │ IsCompleted                │
        │ TimeSpenSeconds            │
        │ ScrollDepth                │
        │ StartedAt                  │
        │ CompletedAt                │
        │ LowEngagement              │
        └────────────────────────────┘
                    │
                    └──────────────┐
                                   │
                    ┌──────────────┴─────────┐
                    │                        │
                    ▼                        ▼
        ┌──────────────────┐    ┌──────────────────┐
        │  QUIZRESULTS     │    │PROGRESSTRACKINGS │
        ├──────────────────┤    ├──────────────────┤
        │ Id (PK)          │    │ Id (PK)          │
        │ QuizId (FK)      │    │ StudentId (FK)   │
        │ StudentId (FK)   │    │ SubjectId (FK)   │
        │ Score            │    │ FinalProgress    │
        │ AttemptDate      │    │ ScreenTime%      │
        │ QuestionCount    │    │ QuizScore%       │
        │ CorrectCount     │    │ Completion%      │
        └──────────────────┘    │ LowEngagement    │
                                │ LastUpdated      │
                                └──────────────────┘
```

### ER Summary: How User Connects
```
USER (Central Entity) connects to:
├─ ADMINS (1:1) → System Administrators
├─ TEACHERS (1:1) → Content Creators who:
│                   └─ Assign to SUBJECTS (via TEACHERSUBJECTS many-to-many)
│                   └─ Upload MATERIALS
├─ STUDENTS (1:1) → Learners who:
│                   └─ Enroll in SEMESTERS (via STUDENTENROLLMENTS many-to-many)
│                   └─ Read MATERIALPAGES (track via MATERIALPAGEPROGRESS)
│                   └─ Attempt QUIZZES (track via QUIZRESULTS)
│                   └─ Track Overall PROGRESSTRACKINGS (by subject/topic)
│
└─ OTPVERIFICATIONS (1:many) → Phone verification during registration
```

## 19. Dashboard Features by Role

### A) Admin Dashboard
**Location:** `/Admin/Dashboard`

**Components:**
1. **Summary Cards (4-5 cards at the top)**
   - Total Users (Count)
   - Total Students (Count)
   - Total Teachers (Count)
   - Total Materials (Count)
   - Total Quizzes (Count)

2. **User Management**
   - View All Users (list by role)
   - Filter by Role (Admin, Teacher, Student, All)
   - See User Details:
     - Full Name
     - Phone Number
     - Role
     - Approval Status (Approved/Pending)
     - Creation Date
     - Teacher ID (if Teacher)
     - Enrollment Number (if Student)
   - Action: Approve/Deny Users
   - Action: Edit/Delete Users

3. **Semester & Subject Management**
   - Create New Semester
   - View All Semesters
   - Edit/Delete Semesters
   - Create Subjects per Semester
   - View All Subjects
   - Edit/Delete Subjects

4. **Teacher-Subject Assignment**
   - Assign Teachers to Subjects
   - View Assignments
   - Edit/Remove Assignments

5. **Enrollment Approval**
   - View Pending Semester Enrollment Requests
   - Approve/Reject Requests
   - See Student Details & Requested Semester

6. **Student Grouping**
   - View Students grouped by Semester
   - See enrollment status for each student

7. **Progress Analytics**
   - System-wide Progress Report
   - View all students' overall progress
   - Filter by semester/subject
   - See progress bands (Skimmer → Mastered)
   - View metrics:
     - Screen Time %
     - Quiz Score %
     - Completion %
     - Final Progress %
   - Identify Low Engagement Students

### B) Teacher Dashboard
**Location:** `/Teacher/Dashboard`

**Components:**

1. **Summary Cards (3-4 cards)**
   - Total Materials Uploaded (Count)
   - Total Quizzes Created (Count)
   - Students with Low Engagement (Count)
   - Recent Progress (Count of recent updates)

2. **Upload Material**
   - Upload New Chapter Material
   - See form fields:
     - Select Semester
     - Select Subject (where teacher is assigned)
     - Material Title
     - Material Description
     - Pages (Add/Remove pages with rich-text editor)
     - Optional Quiz per Page
   - Auto re-number pages on insertion/deletion
   - Save Material

3. **View Materials**
   - List all uploaded materials
   - Show:
     - Material Title
     - Subject
     - Semester
     - Number of Pages
     - Number of attached Quizzes
     - Upload Date
   - Action: Preview Chapters
   - Action: Edit (not fully implemented, but intended)
   - Action: Delete (with safeguards)

4. **Create/Manage Quizzes**
   - Create New Quiz
   - View All Quizzes
   - Edit Quiz Details:
     - Title
     - Subject
     - Questions (Add/Remove)
     - Pass % Threshold
   - Delete Quiz

5. **Student Progress Analytics**
   - View students who used teacher's materials
   - See per-student progress:
     - Student Name
     - Enrollment Number
     - Subject/Topic studied
     - Screen Time % (for this subject)
     - Quiz Score % (for quizzes taken)
     - Completion % (pages completed)
     - Final Progress % (calculated)
     - Engagement Status (Skimmer → Mastered, or Low Engagement flag)
   - Filter by subject
   - Sort by progress or name
   - Identify top performers and struggling students

6. **Preview Chapter**
   - View uploaded material pages
   - See page title and content
   - See quiz (if attached)
   - Read-only view

### C) Student Dashboard
**Location:** `/Student/Dashboard`

**Components:**

1. **Summary Cards (2-3 cards)**
   - Average Screen Time % (across all subjects)
   - Average Quiz Score % (across all subjects)
   - Average Completion % (across all subjects)
   - OR
   - Recent Material Accessed
   - Pending Semester Approval (if applicable)

2. **Enrollment Status**
   - Show enrolled semesters
   - Show enrollment approval status (Approved/Pending)
   - If pending, show "Waiting for Admin Approval" message
   - If approved, show "Access Granted" and link to Library

3. **Library Access**
   - Browse Materials by:
     - Semester (dropdown/filter)
     - Subject (dropdown/filter)
     - Topic (optional nested filter)
   - Search Materials (by title/description)
   - Material Display:
     - Title
     - Description
     - Subject
     - Topic (if set)
     - Teacher Name
     - Number of Pages
     - Number of Quizzes
   - Action: "Start Reading" / "Continue Reading"

4. **Read Material**
   - Display current page content (rich-text/HTML)
   - Show page number (e.g., "Page 2 of 5")
   - Show previous/next page buttons (disabled if not yet completed/timed)
   - Timer display (minutes/seconds spent on this page)
   - Progress bar (completion % for material)
   - "Mark as Complete" button (if 3-min minimum met)
   - Quiz Modal (if quiz attached to page):
     - Display questions
     - Multiple choice/text input options
     - Submit button
     - Score display after submission

5. **Progress Analytics (Student View)**
   - Per-Subject Progress:
     - Subject Name
     - Screen Time %
     - Quiz Score %
     - Completion %
     - Final Progress %
     - Status Badge (Skimmer → Mastered)
   - Overall Progress Chart (visual)
   - Engagement Alerts (if low engagement detected)

6. **Material Progress Tracking**
   - Show materials the student has started
   - Show pages completed / total pages
   - Show quizzes attempted / quizzes available
   - Show overall time spent
   - Link to "Continue" or "Restart"

### D) Common Dashboard Elements (All Roles)

**Top Navigation Bar (in all dashboards):**
- Application Logo/Title
- Current User Name & Role Badge
- Links to role-specific pages (e.g., Dashboard, Users, Upload, Library, etc.)
- Logout button

**Footer:**
- Copyright info
- Quick links (About, Help, Contact, Privacy)
- Developer/Organization info

**Responsive Design:**
- Dashboards are optimized for desktop and mobile views
- Cards/tables are responsive
- Modals for quizzes and confirmations are mobile-friendly

## 17. Conclusion
Smart E-Library provides a clear structure for learning content, quizzes, and progress tracking.

It supports three main user roles (Admin, Teacher, Student) and enables admins and teachers to monitor student engagement and learning outcomes using stored progress data.
