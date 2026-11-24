namespace TraineeTracker;

public static class AppConstants
{
    // Platzhalter bis Identity eingerichtet ist —
    // ersetzt später durch User.FindFirstValue(ClaimTypes.NameIdentifier)
    public const string DevTraineeId = "dev-trainee-01";

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Mentor = "Mentor";
        public const string Trainee = "Trainee";
    }

    public static class RoleValues
    {
        public const string Admin = "admin";
        public const string Mentor = "mentor";
        public const string Trainee = "trainee";
    }

    public static class Cookies
    {
        public const string Role = "role";
        public const string MentorTrainee = "traineeTracker.mentorTrainee";
        public const string Authentication = "TraineeTracker.Auth";
    }

    public static class Progress
    {
        public const decimal HoursPerPersonDay = 8m;
        public const decimal FinishedCompletionFactor = 0.7m;
        public const decimal RejectedCompletionFactor = 0.8m;
        public const decimal CompleteCompletionFactor = 1m;
        public const decimal PercentFactor = 100m;
    }
}
