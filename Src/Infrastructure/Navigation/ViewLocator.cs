using makeBreak.Src.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace makeBreak.Src.Infrastructure.Navigation;

/// <summary>
/// Resolves <c>IViewFor&lt;T&gt;</c> instances from the dependency injection
/// container so that <c>RoutedViewHost</c> can display routed screens.
/// </summary>
public sealed class ViewLocator : IViewLocator
{
    private readonly IServiceProvider _services;

    public ViewLocator(IServiceProvider services) => _services = services;

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    {
        if (viewModel is null)
        {
            return null;
        }

        Type viewType = typeof(IViewFor<>).MakeGenericType(viewModel.GetType());
        return _services.GetService(viewType) as IViewFor;
    }
}