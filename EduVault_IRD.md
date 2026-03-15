# EduVault Information Requirement Document (IRD)

## 1. Project Overview
EduVault is a web-based academic resource management platform where students and teachers can access, upload, and manage study materials, books, and subject resources.

## 2. Objectives of the System
- Centralize academic resources for easy access.
- Enable teachers to upload and manage subject materials.
- Provide students with structured access to learning content.
- Support administrative oversight, approvals, and reporting.
- Ensure secure, role-based access to system functions.

## 3. System Actors
- Admin
- Teacher
- Student

## 4. Information Requirements
- User identity, role, and authentication data.
- Academic structure: semesters, subjects, topics.
- Study materials: books, notes, files, links.
- Resource metadata: title, description, subject, uploader, timestamps.
- Student enrollments and approvals.
- Activity data for progress, usage, and reports.

## 5. Functional Requirements
- User authentication and role-based authorization.
- Admin management of users, semesters, subjects, and approvals.
- Teacher upload and organization of subject resources.
- Student access to resources based on enrollment and approvals.
- Resource search and filtering by subject/semester.
- Reporting and dashboards for admin and teachers.

## 6. Non-Functional Requirements
- Usability: simple, teacher-friendly UI.
- Reliability: consistent data integrity and availability.
- Security: encrypted passwords, controlled access.
- Maintainability: clean MVC structure and modular services.
- Scalability: handle typical institutional usage growth.

## 7. Data Requirements
- Store user profiles with role and approval status.
- Store academic hierarchy (semester, subject, topic).
- Store resources with uploader and subject mapping.
- Store enrollments and approval workflow data.
- Store progress and interaction data for reporting.

## 8. Database Entities
| Entity | Purpose |
| --- | --- |
| Users | Authentication and role-based identity storage. |
| Admins | Admin profile and linkage to user. |
| Teachers | Teacher profile and linkage to user. |
| Students | Student profile and linkage to user. |
| Semesters | Academic semester definitions. |
| Subjects | Subject definitions tied to semesters. |
| Topics | Optional subject sub-grouping. |
| TeacherSubjects | Mapping of teachers to subjects. |
| StudentEnrollments | Student-semester enrollments and approvals. |
| Materials | Uploaded learning resources (notes/files/links). |
| MaterialPages | Pages for rich-text materials. |
| MaterialPageProgress | Student progress on pages. |
| Quizzes | Assessments linked to materials or subjects. |
| QuizQuestions | Quiz question bank. |
| QuizResults | Student quiz attempts and scores. |
| ProgressTrackings | Aggregated learning progress metrics. |
| OtpVerifications | OTP verification data for login/validation. |
| DeletedUsers | Archive of deleted accounts. |
| SemesterResultPublishes | Publish state for semester results. |

## 9. Data Relationships
- One Subject has many Teachers.
- One Subject has many Books.
- One Teacher uploads many Resources.
- One Subject contains many Resources.

## 10. System Modules
- Authentication and Session Management
- Admin Management
- Teacher Resource Management
- Student Resource Access
- Enrollment and Approval Workflow
- Reporting and Analytics
- Result Publication and Viewing

## 11. System Workflow
1. Admin creates semesters and subjects.
2. Admin assigns teachers to subjects.
3. Teachers upload resources by subject.
4. Students request enrollment in semesters.
5. Admin approves enrollments.
6. Students access resources based on approval.
7. System tracks progress and generates reports.

## 12. Security Requirements
- Password hashing and secure authentication.
- Role-based authorization for all endpoints.
- Session management with inactivity timeouts.
- Input validation for all forms.
- Restricted access to admin functions.

## 13. Performance Requirements
- Page load within acceptable classroom use limits.
- Efficient database queries with indexing.
- Minimize large file load delays with streaming or pagination.

## 14. Storage Requirements
- Store uploaded files in structured server directories.
- Maintain resource metadata in MySQL database.
- Retain logs and audit records for admin operations.

## 15. Dashboard Requirements
- Admin dashboard: users, subjects, semesters, approvals, reports.
- Teacher dashboard: uploaded materials, quiz activity, alerts.
- Student dashboard: enrollment status, resources, results.

## 16. Reporting Requirements
- Student progress analytics by subject and semester.
- Quiz performance metrics per class.
- Engagement and completion statistics.

## 17. Future Enhancements
- Advanced search with tagging and filters.
- Real-time notifications for approvals and updates.
- File versioning and resource history.
- Exportable analytics dashboards.

## 18. Technology Requirements
- ASP.NET Core MVC
- MySQL Database
- Entity Framework Core
- Bootstrap 5
- HTML/CSS
