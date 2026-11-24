namespace TraineeTracker.Helpers;

public static class CurriculumLinks
{
    public static string? ForDemo(string? link)
    {
        if (Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Host == "curriculum.example"
            && long.TryParse(uri.Segments.LastOrDefault()?.Trim('/'), out var id))
            return "/DemoLessons/" + id;
        return link;
    }
}
