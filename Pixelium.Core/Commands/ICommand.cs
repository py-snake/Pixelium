namespace Pixelium.Core.Commands
{
    public interface IImageCommand
    {
        string Name { get; }
        bool Execute();
        bool Undo();
    }
}
