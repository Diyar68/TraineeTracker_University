using TraineeTracker.Models;

namespace TraineeTracker.ViewModels;

public class CurriculumImportViewModel
{
    public CurriculumImportResultViewModel? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<Lection> Lessons { get; set; } = [];
    
    public int ActiveLections => Lessons.Count(l => l.IsActive);
    public int InactiveLections => Lessons.Count(l => !l.IsActive);
    public int TotalLections => Lessons.Count;
}

public class CurriculumImportResultViewModel
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Deprecated { get; set; }
    public int Unchanged { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}