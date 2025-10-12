using System.Collections.Generic;

namespace Pixelium.Core.Commands
{
    public class CommandHistory
    {
        private readonly List<IImageCommand> _commands = new();
        private int _currentIndex = -1;
        private const int MaxHistorySize = 50;

        public bool CanUndo => _currentIndex >= 0;
        public bool CanRedo => _currentIndex < _commands.Count - 1;

        public void Execute(IImageCommand command)
        {
            if (command.Execute())
            {
                // Remove any commands after current index (for branching)
                if (_currentIndex < _commands.Count - 1)
                {
                    _commands.RemoveRange(_currentIndex + 1, _commands.Count - _currentIndex - 1);
                }

                _commands.Add(command);
                _currentIndex++;

                // Limit history size
                if (_commands.Count > MaxHistorySize)
                {
                    _commands.RemoveAt(0);
                    _currentIndex--;
                }
            }
        }

        public bool Undo()
        {
            if (CanUndo)
            {
                var command = _commands[_currentIndex];
                if (command.Undo())
                {
                    _currentIndex--;
                    return true;
                }
            }
            return false;
        }

        public bool Redo()
        {
            if (CanRedo)
            {
                _currentIndex++;
                var command = _commands[_currentIndex];
                return command.Execute();
            }
            return false;
        }
    }
}
