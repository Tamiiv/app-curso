namespace AppCurso.Web.Features.Content;

public sealed record LessonHeading(string Id, string Text, int Level);

public sealed record QuizQuestion(
    string Question,
    IReadOnlyList<string> Options,
    int Answer,
    string Explanation)
{
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Question) &&
        Options.Count >= 2 &&
        Answer >= 0 &&
        Answer < Options.Count;
}
