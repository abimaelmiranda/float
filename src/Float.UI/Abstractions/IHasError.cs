namespace Float.UI.Abstractions;

public interface IHasError
{
    bool HasError { get; }
    string ErrorMessage { get; }
}
