using System;
using UnityEngine;

namespace UnityUI.Commands
{
    public class UnityCommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public UnityCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute() => _canExecute == null || _canExecute();

        public void Execute()
        {
            if (CanExecute())
                _execute();
        }
    }
    public class UnityCommand<T>
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;
        public UnityCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public bool CanExecute(T parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(T parameter)
        {
            if (CanExecute(parameter))
                _execute(parameter);
        }
    }
}
