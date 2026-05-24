using System.ComponentModel;

namespace Litenbib.ViewModels
{
    public interface ITaskDialogContentViewModel
    {
        string Title { get; }

        string Heading { get; }

        bool CanApply { get; }
    }

    public class TaskDialogViewModel : ViewModelBase
    {
        public ITaskDialogContentViewModel Content { get; }

        public string Title => Content.Title;

        public string Heading => Content.Heading;

        public bool CanApply => Content.CanApply;

        public TaskDialogViewModel() : this(new BatchFieldDeleteViewModel()) { }

        public TaskDialogViewModel(ITaskDialogContentViewModel content)
        {
            Content = content;
            if (content is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += Content_PropertyChanged;
            }
        }

        private void Content_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(ITaskDialogContentViewModel.CanApply))
            {
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }
}
