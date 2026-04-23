namespace HealthCareMS.Blazor.Services;

public sealed record NavigationMenuModel(
    string Culture,
    IReadOnlyList<NavigationMenuGroupModel> Groups);

public sealed record NavigationMenuGroupModel(
    string Key,
    string Label,
    int SortOrder,
    IReadOnlyList<NavigationMenuItemModel> Items);

public sealed record NavigationMenuItemModel(
    string Key,
    string Label,
    string Icon,
    string? IconLabel,
    string Route,
    int SortOrder,
    IReadOnlyList<NavigationMenuItemModel> Children);
