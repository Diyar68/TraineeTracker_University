using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Filters;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Controllers;

[RequireDemoRole(AppConstants.RoleValues.Mentor, AppConstants.RoleValues.Admin)]
public class CurriculumImportController : Controller
{
    private static readonly HashSet<string> JsonContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "text/json",
        "application/octet-stream"
    };

    private readonly ICurriculumJsonParser _parser;
    private readonly ICurriculumSyncService _syncService;
    private readonly ILectionRepository _lectionRepository;

    public CurriculumImportController(
        ICurriculumJsonParser parser,
        ICurriculumSyncService syncService,
        ILectionRepository lectionRepository)
    {
        _parser = parser;
        _syncService = syncService;
        _lectionRepository = lectionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        SetPageMetadata();
        return View(await BuildViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile? curriculumFile, CancellationToken cancellationToken)
    {
        SetPageMetadata();

        if (curriculumFile is null || curriculumFile.Length == 0)
        {
            return View(await BuildViewModelAsync(
                errorMessage: "Bitte wähle eine JSON-Datei für den Lehrplan-Import aus."));
        }

        if (!IsJsonFile(curriculumFile))
        {
            return View(await BuildViewModelAsync(
                errorMessage: "Bitte lade eine gültige JSON-Datei hoch."));
        }

        try
        {
            await using var stream = curriculumFile.OpenReadStream();
            var importedLections = await _parser.ParseAsync(stream, cancellationToken);
            var result = await _syncService.ImportAsync(1, importedLections, cancellationToken);

            var resultViewModel = new CurriculumImportResultViewModel
            {
                Created = result.Created,
                Updated = result.Updated,
                Deprecated = result.Deprecated,
                Unchanged = result.Unchanged,
                Warnings = result.Warnings
            };

            return View(await BuildViewModelAsync(resultViewModel));
        }
        catch (InvalidDataException ex)
        {
            return View(await BuildViewModelAsync(errorMessage: ex.Message));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return View(await BuildViewModelAsync(
                errorMessage: "Der Lehrplan konnte nicht importiert werden. Bitte prüfe die Datei und versuche es erneut."));
        }
    }

    private static bool IsJsonFile(IFormFile file)
    {
        var hasJsonExtension = string.Equals(Path.GetExtension(file.FileName), ".json", StringComparison.OrdinalIgnoreCase);
        var hasJsonContentType = !string.IsNullOrWhiteSpace(file.ContentType) &&
            JsonContentTypes.Contains(file.ContentType);

        return hasJsonExtension || hasJsonContentType;
    }

    private async Task<CurriculumImportViewModel> BuildViewModelAsync(
        CurriculumImportResultViewModel? result = null,
        string? errorMessage = null)
    {
        var lessons = await _lectionRepository.GetAllAsync();

        return new CurriculumImportViewModel
        {
            Result = result,
            ErrorMessage = errorMessage,
            Lessons = lessons
        };
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "Lehrplan importieren";
        ViewData["Eyebrow"] = "Mentor | Lehrplan";
    }
}