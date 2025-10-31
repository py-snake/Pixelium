namespace Pixelium.Core.Models
{
    /// <summary>
    /// Defines how image edits are applied in the application.
    /// </summary>
    public enum EditMode
    {
        /// <summary>
        /// Modifies the active layer in-place. Uses undo/redo for history.
        /// Suitable for quick edits with minimal memory usage.
        /// </summary>
        Destructive,

        /// <summary>
        /// Creates a new layer for each edit. Each transformation becomes a new layer.
        /// Provides visual history and non-destructive workflow similar to Photoshop.
        /// </summary>
        NonDestructive
    }
}
