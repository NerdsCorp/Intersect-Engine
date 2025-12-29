namespace Intersect.Enums;

/// <summary>
/// Defines permission levels for house visitors.
/// </summary>
public enum HousePermission
{
    /// <summary>
    /// No access to the house.
    /// </summary>
    None = 0,

    /// <summary>
    /// Can view the house but not modify anything.
    /// </summary>
    View = 1,

    /// <summary>
    /// Can view and modify furniture.
    /// </summary>
    Modify = 2,

    /// <summary>
    /// Owner of the house with full permissions.
    /// </summary>
    Owner = 99
}
