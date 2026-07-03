namespace EnterpriseApiAutomationFramework.Core.Helpers;

public sealed class RoleGroupEntry
{
    public required string ParentRole { get; init; }
    public required string ChildRole { get; init; }
    public int ExecutionOrder { get; init; }
    public bool IsEnabled { get; init; }

    public static RoleGroupEntry? FromRow(Dictionary<string, string> row)
    {
        if (!row.TryGetValue(TestConfigDefaults.ParentRoleColumn, out var parent)
            || string.IsNullOrWhiteSpace(parent)
            || !row.TryGetValue(TestConfigDefaults.ChildRoleColumn, out var child)
            || string.IsNullOrWhiteSpace(child))
        {
            return null;
        }

        var order = 0;
        if (row.TryGetValue(TestConfigDefaults.ExecutionOrderColumn, out var orderText)
            && !int.TryParse(orderText, out order))
        {
            order = 0;
        }

        var enabled = true;
        if (row.TryGetValue(TestConfigDefaults.EnabledColumn, out var enabledText)
            && !string.IsNullOrWhiteSpace(enabledText))
        {
            enabled = !string.Equals(enabledText.Trim(), "No", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(enabledText.Trim(), "False", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(enabledText.Trim(), "0", StringComparison.OrdinalIgnoreCase);
        }

        return new RoleGroupEntry
        {
            ParentRole = parent.Trim(),
            ChildRole = child.Trim(),
            ExecutionOrder = order,
            IsEnabled = enabled
        };
    }
}
