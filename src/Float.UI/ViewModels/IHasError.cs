namespace Float.UI.ViewModels;

public interface IHasError
{
    bool HasError { get; }
    string ErrorMessage { get; }
}
