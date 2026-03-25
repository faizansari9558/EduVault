using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Filters;
using SmartELibrary.Models;
using SmartELibrary.Services;
using SmartELibrary.ViewModels;

namespace SmartELibrary.Controllers;

[RoleAuthorize(UserRole.Teacher)]
public class TeacherController(ApplicationDbContext dbContext, IProgressService progressService, IWebHostEnvironment env) : Controller
{
    
    private const int MaxPagePlainTextChars = RichTextContentLengthValidator.MaxPlainTextChars;

    public async Task<IActionResult> Dashboard()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        ViewBag.MaterialCount = await dbContext.Materials.CountAsync(x => x.TeacherId == teacherId);
        ViewBag.QuizCount = await dbContext.Quizzes.CountAsync(x => x.TeacherId == teacherId);

        var subjectIds = await dbContext.TeacherSubjects
            .Where(x => x.TeacherId == teacherId)
            .Select(x => x.SubjectId)
            .ToListAsync();

        ViewBag.LowEngagementCount = await dbContext.ProgressTrackings
            .CountAsync(x => subjectIds.Contains(x.SubjectId) && x.IsLowEngagementAlert);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> UploadMaterial()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var subjIds = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();
        
        var subjects = await dbContext.Subjects
            .Where(x => subjIds.Contains(x.Id))
            .ToListAsync();
        var semesterIds = subjects.Select(x => x.SemesterId).Distinct().ToList();
        var semesters = await dbContext.Semesters
            .Where(x => semesterIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        foreach (var s in subjects)
        {
            if (semesters.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }
        ViewBag.Subjects = subjects;

        ViewBag.MaxPageContentLength = MaxPagePlainTextChars;

        return View(new ChapterUploadViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> UploadMaterial(ChapterUploadViewModel model)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var tSubjIds = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();

        var teacherSubjects = await dbContext.Subjects
            .Where(x => tSubjIds.Contains(x.Id))
            .ToListAsync();
        var semIds = teacherSubjects.Select(x => x.SemesterId).Distinct().ToList();
        var sems = await dbContext.Semesters.Where(x => semIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        foreach (var s in teacherSubjects)
        {
            if (sems.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }

        var selectedSubject = teacherSubjects.FirstOrDefault(x => x.Id == model.SubjectId);
        if (selectedSubject is null)
        {
            ModelState.AddModelError(nameof(model.SubjectId), "Please select a valid assigned subject.");
        }
        else
        {
            model.SemesterId = selectedSubject.SemesterId;
        }

        if (model.Pages.Count < 2)
        {
            ModelState.AddModelError(nameof(model.Pages), "Add at least 2 pages.");
        }

        for (var i = 0; i < model.Pages.Count; i++)
        {
            var page = model.Pages[i];
            if (string.IsNullOrWhiteSpace(page.PageTitle))
            {
                ModelState.AddModelError($"Pages[{i}].PageTitle", "Page title is required.");
            }

            if (string.IsNullOrWhiteSpace(page.HtmlContent))
            {
                ModelState.AddModelError($"Pages[{i}].HtmlContent", "Page content is required.");
            }
            else if (!RichTextContentLengthValidator.IsWithinLimit(page.HtmlContent, out _))
            {
                ModelState.AddModelError($"Pages[{i}].HtmlContent", RichTextContentLengthValidator.TooLongMessage);
            }

            if (page.Questions.Count > 0)
            {
                for (var q = 0; q < page.Questions.Count; q++)
                {
                    var question = page.Questions[q];
                    if (string.IsNullOrWhiteSpace(question.QuestionText) ||
                        string.IsNullOrWhiteSpace(question.OptionA) ||
                        string.IsNullOrWhiteSpace(question.OptionB) ||
                        string.IsNullOrWhiteSpace(question.OptionC) ||
                        string.IsNullOrWhiteSpace(question.OptionD))
                    {
                        ModelState.AddModelError(string.Empty, $"Quiz details are required for page {i + 1}, question {q + 1}.");
                    }

                    var correct = (question.CorrectOption ?? string.Empty).Trim().ToUpperInvariant();
                    if (correct is not ("A" or "B" or "C" or "D"))
                    {
                        ModelState.AddModelError(string.Empty, $"Correct option must be A, B, C, or D for page {i + 1}, question {q + 1}.");
                    }
                }
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Subjects = teacherSubjects;
            ViewBag.MaxPageContentLength = MaxPagePlainTextChars;
            return View(model);
        }

        var material = new Material
        {
            Title = model.Title,
            Description = model.Description,
            MaterialType = MaterialType.Notes,
            SemesterId = model.SemesterId,
            SubjectId = model.SubjectId,
            TopicId = model.TopicId,
            IsPublic = model.IsPublic,
            TeacherId = teacherId,
            FilePathOrUrl = "RICH_TEXT"
        };

        dbContext.Materials.Add(material);
        await dbContext.SaveChangesAsync();

        var pages = new List<MaterialPage>();
        for (var i = 0; i < model.Pages.Count; i++)
        {
            var input = model.Pages[i];
            var page = new MaterialPage
            {
                MaterialId = material.Id,
                PageNumber = i + 1,
                Title = input.PageTitle.Trim(),
                HtmlContent = input.HtmlContent
            };
            pages.Add(page);
            dbContext.MaterialPages.Add(page);
        }

        await dbContext.SaveChangesAsync();

        for (var i = 0; i < model.Pages.Count; i++)
        {
            var input = model.Pages[i];
            if (input.Questions.Count == 0)
            {
                continue;
            }

            var quizTitle = string.IsNullOrWhiteSpace(input.QuizTitle)
                ? $"Quiz - {material.Title} - Page {i + 1}"
                : input.QuizTitle.Trim();

            var quiz = new Quiz
            {
                Title = quizTitle,
                SubjectId = material.SubjectId,
                TopicId = material.TopicId,
                MaterialId = material.Id,
                MaterialPageId = pages[i].Id,
                TeacherId = teacherId,
                QuizQuestions = input.Questions.Select(question => new QuizQuestion
                {
                    QuestionText = question.QuestionText,
                    OptionA = question.OptionA,
                    OptionB = question.OptionB,
                    OptionC = question.OptionC,
                    OptionD = question.OptionD,
                    CorrectOption = (question.CorrectOption ?? "A").Trim().ToUpperInvariant()
                }).ToList()
            };

            dbContext.Quizzes.Add(quiz);
        }

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Materials));
    }

    [HttpGet]
    public async Task<IActionResult> EditMaterial(int materialId)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var material = await dbContext.Materials
            .FirstOrDefaultAsync(x => x.Id == materialId && x.TeacherId == teacherId);

        if (material is null)
        {
            return NotFound();
        }

        material.Pages = await dbContext.MaterialPages
            .Where(x => x.MaterialId == material.Id)
            .OrderBy(x => x.PageNumber)
            .ToListAsync();

        if (!string.Equals(material.FilePathOrUrl, "RICH_TEXT", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only rich text materials can be edited as chapters.";
            return RedirectToAction(nameof(Materials));
        }

        var pageIds = material.Pages.Select(x => x.Id).ToList();
        var quizzes = await dbContext.Quizzes
            .Include(x => x.QuizQuestions)
            .Where(x => x.MaterialPageId.HasValue && pageIds.Contains(x.MaterialPageId.Value))
            .ToListAsync();

        var quizLookup = quizzes.ToDictionary(x => x.MaterialPageId!.Value, x => x);

        var model = new ChapterUploadViewModel
        {
            Title = material.Title,
            Description = material.Description,
            SubjectId = material.SubjectId,
            SemesterId = material.SemesterId,
            TopicId = material.TopicId,
            IsPublic = material.IsPublic,
            Pages = material.Pages
                .OrderBy(x => x.PageNumber)
                .Select(x => new ChapterPageInputViewModel
                {
                    PageId = x.Id,
                    PageTitle = x.Title,
                    HtmlContent = x.HtmlContent,
                    QuizTitle = quizLookup.TryGetValue(x.Id, out var quiz) ? quiz.Title : string.Empty,
                    Questions = quizLookup.TryGetValue(x.Id, out var pageQuiz)
                        ? pageQuiz.QuizQuestions
                            .OrderBy(q => q.Id)
                            .Select(q => new ChapterPageQuestionInputViewModel
                            {
                                QuestionText = q.QuestionText,
                                OptionA = q.OptionA,
                                OptionB = q.OptionB,
                                OptionC = q.OptionC,
                                OptionD = q.OptionD,
                                CorrectOption = q.CorrectOption
                            })
                            .ToList()
                        : new List<ChapterPageQuestionInputViewModel>()
                })
                .ToList()
        };

        var editSubjIds = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();
            
        var subjectsForEdit = await dbContext.Subjects
            .Where(x => editSubjIds.Contains(x.Id))
            .ToListAsync();
            
        var editSemIds = subjectsForEdit.Select(x => x.SemesterId).Distinct().ToList();
        var editSemesters = await dbContext.Semesters
            .Where(x => editSemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
            
        foreach (var s in subjectsForEdit)
        {
            if (editSemesters.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }
        
        ViewBag.Subjects = subjectsForEdit;
        ViewBag.MaxPageContentLength = MaxPagePlainTextChars;
        ViewBag.IsEdit = true;
        ViewBag.MaterialId = materialId;

        return View("UploadMaterial", model);
    }

    [HttpPost]
    public async Task<IActionResult> EditMaterial(int materialId, ChapterUploadViewModel model)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var material = await dbContext.Materials
            .FirstOrDefaultAsync(x => x.Id == materialId && x.TeacherId == teacherId);

        if (material is null)
        {
            return NotFound();
        }

        material.Pages = await dbContext.MaterialPages
            .Where(x => x.MaterialId == material.Id)
            .OrderBy(x => x.PageNumber)
            .ToListAsync();

        if (material is null)
        {
            return NotFound();
        }

        var postEditSubjIds = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();
            
        var teacherSubjects = await dbContext.Subjects
            .Where(x => postEditSubjIds.Contains(x.Id))
            .ToListAsync();
            
        var postEditSemIds = teacherSubjects.Select(x => x.SemesterId).Distinct().ToList();
        var postEditSemesters = await dbContext.Semesters
            .Where(x => postEditSemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
            
        foreach (var s in teacherSubjects)
        {
            if (postEditSemesters.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }

        var selectedSubject = teacherSubjects.FirstOrDefault(x => x.Id == model.SubjectId);
        if (selectedSubject is null)
        {
            ModelState.AddModelError(nameof(model.SubjectId), "Please select a valid assigned subject.");
        }
        else
        {
            model.SemesterId = selectedSubject.SemesterId;
        }

        if (model.Pages.Count < 2)
        {
            ModelState.AddModelError(nameof(model.Pages), "Add at least 2 pages.");
        }

        for (var i = 0; i < model.Pages.Count; i++)
        {
            var page = model.Pages[i];
            if (string.IsNullOrWhiteSpace(page.PageTitle))
            {
                ModelState.AddModelError($"Pages[{i}].PageTitle", "Page title is required.");
            }

            if (string.IsNullOrWhiteSpace(page.HtmlContent))
            {
                ModelState.AddModelError($"Pages[{i}].HtmlContent", "Page content is required.");
            }
            else if (!RichTextContentLengthValidator.IsWithinLimit(page.HtmlContent, out _))
            {
                ModelState.AddModelError($"Pages[{i}].HtmlContent", RichTextContentLengthValidator.TooLongMessage);
            }

            if (page.Questions.Count > 0)
            {
                for (var q = 0; q < page.Questions.Count; q++)
                {
                    var question = page.Questions[q];
                    if (string.IsNullOrWhiteSpace(question.QuestionText) ||
                        string.IsNullOrWhiteSpace(question.OptionA) ||
                        string.IsNullOrWhiteSpace(question.OptionB) ||
                        string.IsNullOrWhiteSpace(question.OptionC) ||
                        string.IsNullOrWhiteSpace(question.OptionD))
                    {
                        ModelState.AddModelError(string.Empty, $"Quiz details are required for page {i + 1}, question {q + 1}.");
                    }

                    var correct = (question.CorrectOption ?? string.Empty).Trim().ToUpperInvariant();
                    if (correct is not ("A" or "B" or "C" or "D"))
                    {
                        ModelState.AddModelError(string.Empty, $"Correct option must be A, B, C, or D for page {i + 1}, question {q + 1}.");
                    }
                }
            }
        }

        var incomingIds = model.Pages
            .Where(x => x.PageId.HasValue)
            .Select(x => x.PageId!.Value)
            .ToHashSet();

        var removedIds = material.Pages
            .Where(x => !incomingIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToList();

        if (removedIds.Count > 0)
        {
            var quizIds = await dbContext.Quizzes
                .Where(x => x.MaterialPageId.HasValue && removedIds.Contains(x.MaterialPageId.Value))
                .Select(x => x.Id)
                .ToListAsync();

            if (quizIds.Count > 0)
            {
                var hasResults = await dbContext.QuizResults.AnyAsync(x => quizIds.Contains(x.QuizId));
                if (hasResults)
                {
                    ModelState.AddModelError(string.Empty, "Cannot remove a page that has quiz attempts.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Subjects = teacherSubjects;
            ViewBag.MaxPageContentLength = MaxPagePlainTextChars;
            ViewBag.IsEdit = true;
            ViewBag.MaterialId = materialId;
            return View("UploadMaterial", model);
        }

        material.Title = model.Title;
        material.Description = model.Description;
        material.SubjectId = model.SubjectId;
        material.SemesterId = model.SemesterId;
        material.TopicId = model.TopicId;
        material.IsPublic = model.IsPublic;

        var newPageQuizzes = new List<(int pageNumber, ChapterPageInputViewModel input)>();

        for (var i = 0; i < model.Pages.Count; i++)
        {
            var input = model.Pages[i];
            var pageNumber = i + 1;
            MaterialPage? targetPage = null;
            if (input.PageId.HasValue)
            {
                targetPage = material.Pages.FirstOrDefault(x => x.Id == input.PageId.Value);
                if (targetPage is not null)
                {
                    targetPage.Title = input.PageTitle.Trim();
                    targetPage.HtmlContent = input.HtmlContent;
                    targetPage.PageNumber = pageNumber;
                }
            }
            else
            {
                targetPage = new MaterialPage
                {
                    MaterialId = material.Id,
                    PageNumber = pageNumber,
                    Title = input.PageTitle.Trim(),
                    HtmlContent = input.HtmlContent
                };
                dbContext.MaterialPages.Add(targetPage);
            }

            if (targetPage is null)
            {
                continue;
            }

            if (targetPage.Id == 0)
            {
                if (input.Questions.Count > 0)
                {
                    newPageQuizzes.Add((pageNumber, input));
                }
                continue;
            }

            var existingQuiz = await dbContext.Quizzes
                .Include(x => x.QuizQuestions)
                .FirstOrDefaultAsync(x => x.MaterialPageId == targetPage.Id);

            if (input.Questions.Count == 0)
            {
                if (existingQuiz is not null)
                {
                    var hasResults = await dbContext.QuizResults.AnyAsync(x => x.QuizId == existingQuiz.Id);
                    if (hasResults)
                    {
                        ModelState.AddModelError(string.Empty, "Cannot remove quiz from a page with quiz attempts.");
                    }
                    else
                    {
                        dbContext.QuizQuestions.RemoveRange(existingQuiz.QuizQuestions);
                        dbContext.Quizzes.Remove(existingQuiz);
                    }
                }

                continue;
            }

            var quizTitle = string.IsNullOrWhiteSpace(input.QuizTitle)
                ? $"Quiz - {material.Title} - Page {pageNumber}"
                : input.QuizTitle.Trim();

            if (existingQuiz is null)
            {
                existingQuiz = new Quiz
                {
                    Title = quizTitle,
                    SubjectId = material.SubjectId,
                    TopicId = material.TopicId,
                    MaterialId = material.Id,
                    MaterialPageId = targetPage.Id,
                    TeacherId = teacherId
                };
                dbContext.Quizzes.Add(existingQuiz);
            }
            else
            {
                existingQuiz.Title = quizTitle;
            }

            if (existingQuiz.QuizQuestions.Count > 0)
            {
                dbContext.QuizQuestions.RemoveRange(existingQuiz.QuizQuestions);
            }

            existingQuiz.QuizQuestions = input.Questions.Select(question => new QuizQuestion
            {
                QuestionText = question.QuestionText,
                OptionA = question.OptionA,
                OptionB = question.OptionB,
                OptionC = question.OptionC,
                OptionD = question.OptionD,
                CorrectOption = (question.CorrectOption ?? "A").Trim().ToUpperInvariant()
            }).ToList();
        }

        if (removedIds.Count > 0)
        {
            var quizzesToRemove = await dbContext.Quizzes
                .Where(x => x.MaterialPageId.HasValue && removedIds.Contains(x.MaterialPageId.Value))
                .Include(x => x.QuizQuestions)
                .ToListAsync();

            if (quizzesToRemove.Count > 0)
            {
                var questions = quizzesToRemove.SelectMany(x => x.QuizQuestions).ToList();
                dbContext.QuizQuestions.RemoveRange(questions);
                dbContext.Quizzes.RemoveRange(quizzesToRemove);
            }

            var toRemove = material.Pages.Where(x => removedIds.Contains(x.Id)).ToList();
            dbContext.MaterialPages.RemoveRange(toRemove);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Subjects = teacherSubjects;
            ViewBag.MaxPageContentLength = MaxPagePlainTextChars;
            ViewBag.IsEdit = true;
            ViewBag.MaterialId = materialId;
            return View("UploadMaterial", model);
        }

        await dbContext.SaveChangesAsync();

        if (newPageQuizzes.Count > 0)
        {
            foreach (var (pageNumber, input) in newPageQuizzes)
            {
                var page = await dbContext.MaterialPages
                    .FirstOrDefaultAsync(x => x.MaterialId == material.Id && x.PageNumber == pageNumber);

                if (page is null)
                {
                    continue;
                }

                var quizTitle = string.IsNullOrWhiteSpace(input.QuizTitle)
                    ? $"Quiz - {material.Title} - Page {pageNumber}"
                    : input.QuizTitle.Trim();

                var quiz = new Quiz
                {
                    Title = quizTitle,
                    SubjectId = material.SubjectId,
                    TopicId = material.TopicId,
                    MaterialId = material.Id,
                    MaterialPageId = page.Id,
                    TeacherId = teacherId,
                    QuizQuestions = input.Questions.Select(question => new QuizQuestion
                    {
                        QuestionText = question.QuestionText,
                        OptionA = question.OptionA,
                        OptionB = question.OptionB,
                        OptionC = question.OptionC,
                        OptionD = question.OptionD,
                        CorrectOption = (question.CorrectOption ?? "A").Trim().ToUpperInvariant()
                    }).ToList()
                };

                dbContext.Quizzes.Add(quiz);
            }

            await dbContext.SaveChangesAsync();
        }

        TempData["Success"] = "Material updated.";
        return RedirectToAction(nameof(Materials));
    }

    public async Task<IActionResult> Materials()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var materialsRaw = await dbContext.Materials
            .Where(x => x.TeacherId == teacherId)
            .ToListAsync();

        var semesterIds = materialsRaw.Select(x => x.SemesterId).Distinct().ToList();
        var semesters = await dbContext.Semesters.Where(x => semesterIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        
        var subjectIds = materialsRaw.Select(x => x.SubjectId).Distinct().ToList();
        var subjects = await dbContext.Subjects.Where(x => subjectIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        
        var materialIds = materialsRaw.Select(x => x.Id).ToList();
        var allPages = await dbContext.MaterialPages.Where(x => materialIds.Contains(x.MaterialId)).ToListAsync();
        var pagesByMaterial = allPages.GroupBy(x => x.MaterialId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var m in materialsRaw)
        {
            if (semesters.TryGetValue(m.SemesterId, out var sem)) m.Semester = sem;
            if (subjects.TryGetValue(m.SubjectId, out var sub)) m.Subject = sub;
            if (pagesByMaterial.TryGetValue(m.Id, out var pages)) m.Pages = pages;
        }

        var materials = materialsRaw.OrderByDescending(x => x.UploadedAtUtc).ToList();
        return View(materials);
    }

    // ── File-based material upload (PDF / PPT / Image) ────────────────────

    [HttpGet]
    public async Task<IActionResult> UploadFile()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var subIdsList = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();
        
        var subjectsList = await dbContext.Subjects
            .Where(x => subIdsList.Contains(x.Id))
            .ToListAsync();
            
        var semesterIdsBySub = subjectsList.Select(x => x.SemesterId).Distinct().ToList();
        var semestersBySub = await dbContext.Semesters
            .Where(x => semesterIdsBySub.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
            
        foreach (var s in subjectsList)
        {
            if (semestersBySub.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }

        ViewBag.Subjects = subjectsList
            .OrderBy(x => x.Semester?.Name ?? "")
            .ThenBy(x => x.Name)
            .ToList();
        return View(new MaterialUploadViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(MaterialUploadViewModel model)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var teacherSubIds = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();

        var teacherSubjectsList = await dbContext.Subjects
            .Where(x => teacherSubIds.Contains(x.Id))
            .ToListAsync();
            
        var teacherSemIds = teacherSubjectsList.Select(x => x.SemesterId).Distinct().ToList();
        var teacherSemesters = await dbContext.Semesters
            .Where(x => teacherSemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
            
        foreach (var s in teacherSubjectsList)
        {
            if (teacherSemesters.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }

        var teacherSubjects = teacherSubjectsList
            .OrderBy(x => x.Semester?.Name ?? "")
            .ThenBy(x => x.Name)
            .ToList();

        var selectedSubject = teacherSubjects.FirstOrDefault(x => x.Id == model.SubjectId);
        if (selectedSubject is null)
            ModelState.AddModelError(nameof(model.SubjectId), "Please select a valid assigned subject.");
        else
            model.SemesterId = selectedSubject.SemesterId;

        if (model.MaterialType == MaterialType.ExternalLink)
        {
            if (string.IsNullOrWhiteSpace(model.ExternalUrl))
                ModelState.AddModelError(nameof(model.ExternalUrl), "URL is required for External Link type.");
        }
        else
        {
            if (model.File is null || model.File.Length == 0)
                ModelState.AddModelError(nameof(model.File), "Please select a file to upload.");
            else if (model.File.Length > 50 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.File), "File size must not exceed 50 MB.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Subjects = teacherSubjects;
            return View(model);
        }

        string filePathOrUrl;

        if (model.MaterialType == MaterialType.ExternalLink)
        {
            filePathOrUrl = model.ExternalUrl!.Trim();
        }
        else
        {
            var ext = Path.GetExtension(model.File!.FileName ?? string.Empty).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var uploadDir = Path.Combine(env.WebRootPath, "uploads", "materials");
            Directory.CreateDirectory(uploadDir);
            var fullPath = Path.Combine(uploadDir, fileName);
            await using var fs = new FileStream(fullPath, FileMode.Create);
            await model.File.CopyToAsync(fs);
            filePathOrUrl = $"/uploads/materials/{fileName}";
        }

        dbContext.Materials.Add(new Material
        {
            Title         = model.Title.Trim(),
            Description   = model.Description?.Trim() ?? string.Empty,
            MaterialType  = model.MaterialType,
            SemesterId    = model.SemesterId,
            SubjectId     = model.SubjectId,
            TopicId       = model.TopicId,
            IsPublic      = model.IsPublic,
            TeacherId     = teacherId,
            FilePathOrUrl = filePathOrUrl
        });

        await dbContext.SaveChangesAsync();

        TempData["Success"] = "File uploaded successfully.";
        return RedirectToAction(nameof(Materials));
    }

    [HttpGet]
    public async Task<IActionResult> PreviewChapter(int materialId, int pageNumber = 1)
    {
        var material = await dbContext.Materials
            .FirstOrDefaultAsync(x => x.Id == materialId);

        if (material is null)
        {
            return NotFound();
        }

        var totalPages = await dbContext.MaterialPages.CountAsync(x => x.MaterialId == materialId);
        if (totalPages == 0)
        {
            return RedirectToAction(nameof(Materials));
        }

        pageNumber = Math.Clamp(pageNumber, 1, totalPages);

        var page = await dbContext.MaterialPages
            .Where(x => x.MaterialId == materialId && x.PageNumber == pageNumber)
            .FirstOrDefaultAsync();

        if (page is null)
        {
            return NotFound();
        }

        var model = new ReadMaterialPageViewModel
        {
            MaterialId = materialId,
            MaterialTitle = material.Title,
            PageId = page.Id,
            PageNumber = page.PageNumber,
            TotalPages = totalPages,
            PageTitle = page.Title,
            HtmlContent = page.HtmlContent,
            HasMandatoryQuizAfterThisPage = false,
            QuizPassed = true,
            QuizId = null
        };

        return View(model);
    }

    // ── Standalone Create Quiz ─────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> CreateQuiz()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var createQuizSubIds = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();

        var createQuizSubjects = await dbContext.Subjects
            .Where(x => createQuizSubIds.Contains(x.Id))
            .ToListAsync();
            
        var createQuizSemIds = createQuizSubjects.Select(x => x.SemesterId).Distinct().ToList();
        var createQuizSemesters = await dbContext.Semesters
            .Where(x => createQuizSemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
            
        foreach (var s in createQuizSubjects)
        {
            if (createQuizSemesters.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }

        ViewBag.Subjects = createQuizSubjects
            .OrderBy(x => x.Semester?.Name ?? "")
            .ThenBy(x => x.Name)
            .ToList();
        return View(new StandaloneQuizViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz(StandaloneQuizViewModel model)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var teacherQuizSubIds = await dbContext.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync();

        var teacherQuizSubjectsList = await dbContext.Subjects
            .Where(x => teacherQuizSubIds.Contains(x.Id))
            .ToListAsync();
            
        var teacherQuizSemIds = teacherQuizSubjectsList.Select(x => x.SemesterId).Distinct().ToList();
        var teacherQuizSemesters = await dbContext.Semesters
            .Where(x => teacherQuizSemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
            
        foreach (var s in teacherQuizSubjectsList)
        {
            if (teacherQuizSemesters.TryGetValue(s.SemesterId, out var sem)) s.Semester = sem;
        }

        var teacherSubjects = teacherQuizSubjectsList
            .OrderBy(x => x.Semester?.Name ?? "")
            .ThenBy(x => x.Name)
            .ToList();

        var selectedSubject = teacherSubjects.FirstOrDefault(x => x.Id == model.SubjectId);
        if (selectedSubject is null)
            ModelState.AddModelError(nameof(model.SubjectId), "Please select a valid assigned subject.");
        else
            model.SemesterId = selectedSubject.SemesterId;

        if (model.Questions.Count == 0)
            ModelState.AddModelError(string.Empty, "Add at least one question.");

        for (var i = 0; i < model.Questions.Count; i++)
        {
            var q = model.Questions[i];
            if (string.IsNullOrWhiteSpace(q.QuestionText))
                ModelState.AddModelError($"Questions[{i}].QuestionText", $"Question {i + 1}: text is required.");
            if (string.IsNullOrWhiteSpace(q.OptionA))
                ModelState.AddModelError($"Questions[{i}].OptionA", $"Question {i + 1}: Option A is required.");
            if (string.IsNullOrWhiteSpace(q.OptionB))
                ModelState.AddModelError($"Questions[{i}].OptionB", $"Question {i + 1}: Option B is required.");
            if (string.IsNullOrWhiteSpace(q.OptionC))
                ModelState.AddModelError($"Questions[{i}].OptionC", $"Question {i + 1}: Option C is required.");
            if (string.IsNullOrWhiteSpace(q.OptionD))
                ModelState.AddModelError($"Questions[{i}].OptionD", $"Question {i + 1}: Option D is required.");
            var correct = (q.CorrectOption ?? string.Empty).Trim().ToUpperInvariant();
            if (correct is not ("A" or "B" or "C" or "D"))
                ModelState.AddModelError($"Questions[{i}].CorrectOption", $"Question {i + 1}: correct option must be A, B, C, or D.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Subjects = teacherSubjects;
            return View(model);
        }

        var quiz = new Quiz
        {
            Title = model.Title.Trim(),
            SubjectId = model.SubjectId,
            TeacherId = teacherId,
            TimeLimitMinutes = model.TimeLimitMinutes,
            AvailableFromUtc = model.AvailableFrom.HasValue
                ? DateTime.SpecifyKind(model.AvailableFrom.Value, DateTimeKind.Local).ToUniversalTime()
                : null,
            AvailableToUtc = model.AvailableTo.HasValue
                ? DateTime.SpecifyKind(model.AvailableTo.Value, DateTimeKind.Local).ToUniversalTime()
                : null,
            QuizQuestions = model.Questions.Select(q => new QuizQuestion
            {
                QuestionText = q.QuestionText.Trim(),
                OptionA = q.OptionA.Trim(),
                OptionB = q.OptionB.Trim(),
                OptionC = q.OptionC.Trim(),
                OptionD = q.OptionD.Trim(),
                CorrectOption = (q.CorrectOption ?? "A").Trim().ToUpperInvariant()
            }).ToList()
        };

        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();
        TempData["Success"] = "Quiz created successfully.";
        return RedirectToAction(nameof(Quizzes));
    }

    public async Task<IActionResult> Quizzes()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var quizzesRaw = await dbContext.Quizzes
            .Where(x => x.TeacherId == teacherId && x.MaterialPageId == null)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        var subjectIds = quizzesRaw.Select(x => x.SubjectId).Distinct().ToList();
        var subjects = await dbContext.Subjects.Where(x => subjectIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var semesterIds = subjects.Values.Select(x => x.SemesterId).Distinct().ToList();
        var semesters = await dbContext.Semesters.Where(x => semesterIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        
        var quizIds = quizzesRaw.Select(x => x.Id).ToList();
        var questions = (await dbContext.QuizQuestions.Where(x => quizIds.Contains(x.QuizId)).ToListAsync())
            .GroupBy(x => x.QuizId).ToDictionary(g => g.Key, g => g.ToList());
        var results = (await dbContext.QuizResults.Where(x => quizIds.Contains(x.QuizId)).ToListAsync())
            .GroupBy(x => x.QuizId).ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var q in quizzesRaw)
        {
            if (subjects.TryGetValue(q.SubjectId, out var sub)) 
            {
                q.Subject = sub;
                if (semesters.TryGetValue(sub.SemesterId, out var sem)) sub.Semester = sem;
            }
            if (questions.TryGetValue(q.Id, out var qs)) q.QuizQuestions = qs;
            if (results.TryGetValue(q.Id, out var rs)) q.QuizResults = rs;
        }
        var quizzes = quizzesRaw;

        return View(quizzes);
    }

    // ── Quiz Results (marks) ────────────────────────────────────────────────

    public async Task<IActionResult> QuizResults(int quizId)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;

        var quizRaw = await dbContext.Quizzes
            .FirstOrDefaultAsync(x => x.Id == quizId && x.TeacherId == teacherId);

        if (quizRaw == null) return NotFound();

        quizRaw.Subject = await dbContext.Subjects.FindAsync(quizRaw.SubjectId);
        quizRaw.QuizQuestions = await dbContext.QuizQuestions.Where(x => x.QuizId == quizRaw.Id).ToListAsync();

        var quiz = quizRaw;

        if (quiz is null)
            return NotFound();

        var results = await dbContext.QuizResults
            .Where(x => x.QuizId == quizId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync();

        var studentIds = results.Select(x => x.StudentId).Distinct().ToList();

        var students = await dbContext.Users
            .Where(x => studentIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        var enrollmentNumbers = await dbContext.Students
            .Where(x => studentIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.EnrollmentNumber })
            .ToDictionaryAsync(x => x.UserId, x => x.EnrollmentNumber);

        // Per student: keep only their best/latest attempt
        var bestResults = results
            .GroupBy(x => x.StudentId)
            .Select(g => g.OrderByDescending(r => r.ScorePercent).ThenByDescending(r => r.SubmittedAtUtc).First())
            .OrderByDescending(r => r.ScorePercent)
            .Select(r => new QuizResultRowViewModel
            {
                StudentName   = students.TryGetValue(r.StudentId, out var name) ? name : "Unknown",
                EnrollmentNo  = enrollmentNumbers.TryGetValue(r.StudentId, out var no) ? no ?? "-" : "-",
                CorrectAnswers = r.CorrectAnswers,
                TotalQuestions = r.TotalQuestions > 0 ? r.TotalQuestions : quiz.QuizQuestions.Count,
                ScorePercent  = Math.Round(r.ScorePercent, 2),
                SubmittedAtUtc = r.SubmittedAtUtc,
                IsAutoSubmitted = r.IsAutoSubmitted,
                AntiCheatReason = r.AntiCheatReason,
                AntiCheatDetectedAtUtc = r.AntiCheatDetectedAtUtc
            })
            .ToList();

        var model = new QuizResultsPageViewModel
        {
            QuizId           = quiz.Id,
            QuizTitle        = quiz.Title,
            SubjectName      = quiz.Subject?.Name ?? "-",
            TotalQuestions   = quiz.QuizQuestions.Count,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            AvailableFromUtc = quiz.AvailableFromUtc,
            AvailableToUtc   = quiz.AvailableToUtc,
            Results          = bestResults
        };

        return View(model);
    }

    public async Task<IActionResult> Progress()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;

        var materialIds = await dbContext.Materials
            .Where(x => x.TeacherId == teacherId)
            .Select(x => x.Id)
            .ToListAsync();

        var pagesForProgressRaw = await dbContext.MaterialPages
            .Where(x => materialIds.Contains(x.MaterialId))
            .ToListAsync();
        
        var matIdsInPages = pagesForProgressRaw.Select(x => x.MaterialId).Distinct().ToList();
        var materialsDict = await dbContext.Materials.Where(x => matIdsInPages.Contains(x.Id)).ToDictionaryAsync(m => m.Id);
        foreach (var p in pagesForProgressRaw)
        {
            if (materialsDict.TryGetValue(p.MaterialId, out var m)) p.Material = m;
        }
        var pagesForProgress = pagesForProgressRaw;

        var totalPagesBySemester = pagesForProgress
            .GroupBy(x => x.Material!.SemesterId)
            .ToDictionary(g => g.Key, g => g.Count());

        var pageProgressRaw = await dbContext.MaterialPageProgress
            .ToListAsync();
        
        // Filter in memory for materialIds
        var pageProgress = pageProgressRaw
            .Where(x => {
                var page = pagesForProgress.FirstOrDefault(p => p.Id == x.MaterialPageId);
                return page != null && materialIds.Contains(page.MaterialId);
            })
            .ToList();

        var studentIdsInProgress = pageProgress.Select(x => x.StudentId).Distinct().ToList();
        var studentsInProgress = await dbContext.Users.Where(u => studentIdsInProgress.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
        
        foreach (var pp in pageProgress)
        {
             if (studentsInProgress.TryGetValue(pp.StudentId, out var s)) pp.Student = s;
             if (pagesForProgress.FirstOrDefault(p => p.Id == pp.MaterialPageId) is { } page) pp.MaterialPage = page;
        }

        var pageQuizIds = await dbContext.Quizzes
            .Where(x => x.MaterialPageId != null && materialIds.Contains(x.MaterialId ?? 0))
            .Select(x => x.Id)
            .ToListAsync();

        var quizQuestionsForProgressGroups = pageQuizIds.Count == 0
            ? new List<QuizQuestion>()
            : await dbContext.QuizQuestions
                .Where(x => pageQuizIds.Contains(x.QuizId))
                .ToListAsync();

        var quizQuestionCounts = quizQuestionsForProgressGroups
                .GroupBy(x => x.QuizId)
                .ToDictionary(g => g.Key, g => g.Count());

        var quizResultsRaw = await dbContext.QuizResults.ToListAsync();
        var quizIdsInResults = quizResultsRaw.Select(r => r.QuizId).Distinct().ToList();
        var quizzesInResults = await dbContext.Quizzes.Where(q => quizIdsInResults.Contains(q.Id)).ToDictionaryAsync(q => q.Id);
        
        foreach (var qr in quizResultsRaw)
        {
            if (quizzesInResults.TryGetValue(qr.QuizId, out var q)) 
            {
                qr.Quiz = q;
                if (q.MaterialId.HasValue && materialsDict.TryGetValue(q.MaterialId.Value, out var m)) q.Material = m;
            }
        }

        var quizResults = quizResultsRaw
            .Where(x => x.Quiz?.MaterialPageId != null && materialIds.Contains(x.Quiz.MaterialId ?? 0))
            .ToList();

        var totalQuizQuestionsBySemester = quizQuestionsForProgressGroups
            .Select(x => {
                if (quizzesInResults.TryGetValue(x.QuizId, out var q)) x.Quiz = q;
                return x;
            })
            .Where(x => x.Quiz?.Material != null && x.Quiz.MaterialPageId != null)
            .GroupBy(x => x.Quiz!.Material!.SemesterId)
            .ToDictionary(g => g.Key, g => g.Count());

        var studentIds = pageProgress.Select(x => x.StudentId)
            .Concat(quizResults.Select(x => x.StudentId))
            .Distinct()
            .ToList();

        var includedSemesters = await dbContext.Semesters
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var students = await dbContext.Users
            .Where(x => studentIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName })
            .ToDictionaryAsync(x => x.Id, x => x);

        var studentEnrollmentNumbers = await dbContext.Students
            .Where(x => studentIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.EnrollmentNumber })
            .ToDictionaryAsync(x => x.UserId, x => x.EnrollmentNumber);

        var enrollmentsRaw = await dbContext.StudentEnrollments
            .Where(x => studentIds.Contains(x.StudentId))
            .ToListAsync();
            
        var eSemIds = enrollmentsRaw.Select(x => x.SemesterId).Distinct().ToList();
        var eSemesters = await dbContext.Semesters.Where(x => eSemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        foreach (var e in enrollmentsRaw)
        {
            if (eSemesters.TryGetValue(e.SemesterId, out var sem)) e.Semester = sem;
        }
        var enrollments = enrollmentsRaw.OrderBy(x => x.SemesterId).ToList();

        var enrollmentsByStudentId = enrollments
            .GroupBy(x => x.StudentId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var rows = new List<ProgressAnalyticsRowViewModel>();

        foreach (var studentId in studentIds)
        {
            var studentName = pageProgress.FirstOrDefault(x => x.StudentId == studentId)?.Student?.FullName
                              ?? (students.TryGetValue(studentId, out var student) ? student.FullName : null)
                              ?? "Unknown";

            var studentEnrollmentNo = studentEnrollmentNumbers.TryGetValue(studentId, out var enrollment)
                ? enrollment
                : null;

            var enrollmentNo = string.IsNullOrWhiteSpace(studentEnrollmentNo) ? "-" : studentEnrollmentNo;

            enrollmentsByStudentId.TryGetValue(studentId, out var studentEnrollments);
            studentEnrollments ??= [];

            var semester = studentEnrollments.Count switch
            {
                0 => "-",
                1 => studentEnrollments[0].Semester?.Name
                     ?? (includedSemesters.TryGetValue(studentEnrollments[0].SemesterId, out var name) ? name : "-"),
                _ => "Multiple"
            };

            var studentSemesterIds = studentEnrollments.Select(x => x.SemesterId).ToList();
            var totalPages = studentSemesterIds.Count > 0 
                ? studentSemesterIds.Sum(sId => totalPagesBySemester.TryGetValue(sId, out var p) ? p : 0) 
                : totalPagesBySemester.Values.Sum();
            var totalQuizQuestions = studentSemesterIds.Count > 0
                ? studentSemesterIds.Sum(sId => totalQuizQuestionsBySemester.TryGetValue(sId, out var q) ? q : 0)
                : totalQuizQuestionsBySemester.Values.Sum();

            var studentPageProgress = pageProgress.Where(x => x.StudentId == studentId).ToList();
            var totalTimeSeconds = studentPageProgress.Sum(x => x.TimeSpentSeconds);
            var screenTimeMinutes = totalTimeSeconds / 60d;

            var effectiveTimeSecondsForFormula = studentPageProgress.Sum(p =>
            {
                var depth = Math.Clamp(p.MaxScrollDepthPercent, 0d, 100d);
                var factor = depth < 30d ? (depth / 30d) : 1d;
                return Math.Max(0d, p.TimeSpentSeconds) * Math.Clamp(factor, 0d, 1d);
            });

            var visitedPages = studentPageProgress
                .Select(x => x.MaterialPageId)
                .Distinct()
                .Count();

            var completedPages = studentPageProgress.Count(x => x.IsCompleted);
            var completionPercent = totalPages == 0 ? 0d : Math.Clamp((double)completedPages / totalPages * 100d, 0, 100);

            var quizCorrectAnswers = quizResults
                .Where(x => x.StudentId == studentId)
                .GroupBy(x => x.QuizId)
                .Select(g => g.OrderByDescending(r => r.SubmittedAtUtc).First())
                .Select(attempt =>
                {
                    var perQuizTotal = attempt.TotalQuestions > 0
                        ? attempt.TotalQuestions
                        : (quizQuestionCounts.TryGetValue(attempt.QuizId, out var count) ? count : 0);

                    var perQuizCorrect = attempt.CorrectAnswers;
                    if (perQuizCorrect <= 0 && perQuizTotal > 0)
                    {
                        perQuizCorrect = (int)Math.Round((double)attempt.ScorePercent / 100d * perQuizTotal);
                    }

                    return Math.Max(0, perQuizCorrect);
                })
                .Sum();

            // If quiz not attempted -> correct remains 0, total is based on created questions.
            // If no quiz exists -> total=0 and score=0.
            var quizScorePercent = totalQuizQuestions <= 0
                ? 0d
                : Math.Clamp(Math.Round((double)quizCorrectAnswers / totalQuizQuestions * 100d, 2), 0, 100);

            var studentViolations = quizResults
                .Where(x => x.StudentId == studentId && x.IsAutoSubmitted)
                .OrderByDescending(x => x.AntiCheatDetectedAtUtc ?? x.SubmittedAtUtc)
                .ToList();

            var latestViolation = studentViolations.FirstOrDefault();

            var averageScrollDepth = studentPageProgress.Count == 0
                ? 0d
                : studentPageProgress.Average(x => Math.Clamp(x.MaxScrollDepthPercent, 0d, 100d));

            var breakdown = progressService.CalculateBreakdown(
                totalActiveReadingSeconds: totalTimeSeconds,
                effectiveActiveReadingSecondsForFormula: effectiveTimeSecondsForFormula,
                totalPages: totalPages,
                completedPages: completedPages,
                averageQuizScorePercent: quizScorePercent,
                averageScrollDepthPercent: averageScrollDepth,
                idealMinutesPerPage: 6d);

            var finalProgress = breakdown.FinalProgressPercent;

            var status = progressService.GetProgressStatus(finalProgress);
            var barClass = status switch
            {
                "Skimmer" => "bg-danger",
                "NeedsImprovement" => "bg-warning",
                "Learning" => "bg-warning",
                "Progressing" => "bg-primary",
                "ActiveLearner" => "bg-success",
                "Mastered" => "bg-success",
                _ => "bg-secondary"
            };

            rows.Add(new ProgressAnalyticsRowViewModel
            {
                EnrollmentNo = enrollmentNo,
                StudentName = studentName,
                Semester = semester,
                ScreenTimeMinutes = Math.Round(screenTimeMinutes, 2),
                ScreenTimeHms = DurationFormatter.ToHms(totalTimeSeconds),
                QuizScorePercent = Math.Round(quizScorePercent, 2),
                QuizMarks = $"{quizCorrectAnswers}/{totalQuizQuestions}",
                QuizViolationCount = studentViolations.Count,
                LatestQuizViolationReason = latestViolation?.AntiCheatReason,
                LatestQuizViolationAtUtc = latestViolation?.AntiCheatDetectedAtUtc ?? latestViolation?.SubmittedAtUtc,
                CompletionPercent = Math.Round(completionPercent, 2),
                FinalProgressPercent = Math.Round(finalProgress, 2),
                Status = status,
                ProgressBarClass = barClass
            });
        }

        rows = rows
            .OrderByDescending(x => x.FinalProgressPercent)
            .ThenBy(x => x.StudentName)
            .ToList();

        var visitedPairsCount = pageProgress
            .Select(x => $"{x.StudentId}:{x.MaterialPageId}")
            .Distinct()
            .Count();

        var model = new ProgressAnalyticsDashboardViewModel
        {
            Rows = rows,
            AverageScreenTimeMinutes = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.ScreenTimeMinutes), 2),
            AverageScreenTimePercentForFormula = rows.Count == 0
                ? 0
                : Math.Round(Math.Clamp(pageProgress.Sum(x => x.TimeSpentSeconds) / (Math.Max(visitedPairsCount, 1) * 360d) * 100d, 0d, 100d), 2),
            AverageQuizScorePercent = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.QuizScorePercent), 2),
            AverageCompletionPercent = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.CompletionPercent), 2)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var material = await dbContext.Materials
            .FirstOrDefaultAsync(x => x.Id == id && x.TeacherId == teacherId);

        if (material is null)
        {
            return NotFound();
        }

        if (!string.Equals(material.FilePathOrUrl, "RICH_TEXT", StringComparison.OrdinalIgnoreCase) && 
            !material.FilePathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            // Try to physical delete the uploaded file
            var filePath = Path.Combine(env.WebRootPath, material.FilePathOrUrl.TrimStart('/', '\\'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        dbContext.Materials.Remove(material);
        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Material deleted successfully.";
        return RedirectToAction(nameof(Materials));
    }

    private static (string status, string barClass) GetStatus(double finalProgress)
    {
        if (finalProgress >= 80)
        {
            return ("Excellent", "bg-success");
        }

        if (finalProgress >= 60)
        {
            return ("Good", "bg-primary");
        }

        if (finalProgress >= 40)
        {
            return ("Average", "bg-warning");
        }

        return ("Needs Improvement", "bg-danger");
    }
}
