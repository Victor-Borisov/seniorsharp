using SeniorSharp.Contracts;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Classifies a candidate's answer against a skill node, emitting graded
/// recognition/application/depth signals plus supporting evidence.
/// </summary>
public interface IClassifier
{
    Task<ClassifierResponse> ClassifyAsync(ClassifierRequest request, CancellationToken ct = default);
}
