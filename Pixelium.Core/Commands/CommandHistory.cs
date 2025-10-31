using System;
using System.Collections.Generic;

namespace Pixelium.Core.Commands
{
    public class CommandHistory : IDisposable
    {
        private readonly List<IImageCommand> _commands = new();
        private readonly object _lock = new object();
        private int _currentIndex = -1;
        private const int MaxHistorySize = 50;
        private bool _disposed = false;

        public bool CanUndo
        {
            get
            {
                lock (_lock)
                {
                    return _currentIndex >= 0;
                }
            }
        }

        public bool CanRedo
        {
            get
            {
                lock (_lock)
                {
                    return _currentIndex < _commands.Count - 1;
                }
            }
        }

        public void Execute(IImageCommand command)
        {
            lock (_lock)
            {
                if (command.Execute())
                {
                    // Remove and dispose any commands after current index (for branching)
                    if (_currentIndex < _commands.Count - 1)
                    {
                        for (int i = _currentIndex + 1; i < _commands.Count; i++)
                        {
                            if (_commands[i] is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
                        _commands.RemoveRange(_currentIndex + 1, _commands.Count - _currentIndex - 1);
                    }

                    _commands.Add(command);
                    _currentIndex++;

                    // Limit history size - dispose oldest command
                    if (_commands.Count > MaxHistorySize)
                    {
                        if (_commands[0] is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        _commands.RemoveAt(0);
                        _currentIndex--;
                    }
                }
            }
        }

        public bool Undo()
        {
            lock (_lock)
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
        }

        public bool Redo()
        {
            lock (_lock)
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

        public void Clear()
        {
            lock (_lock)
            {
                foreach (var command in _commands)
                {
                    if (command is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _commands.Clear();
                _currentIndex = -1;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }
}
