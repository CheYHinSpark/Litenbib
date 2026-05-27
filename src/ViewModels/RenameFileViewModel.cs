using CommunityToolkit.Mvvm.ComponentModel;
using Litenbib.Models;
using System;
using System.IO;

namespace Litenbib.ViewModels
{
    public partial class RenameFileViewModel : ViewModelBase, ITaskDialogContentViewModel, ITaskDialogContentSizing
    {
        private readonly string originalPath;

        public string Title => I18n.Get("Rename.Title");

        public string Heading => I18n.Get("Rename.Heading");

        public double DialogWidth => 520;

        public double DialogHeight => 220;

        public string DirectoryPath { get; }

        public string OriginalFileName { get; }

        public int BaseNameLength { get; }

        [ObservableProperty]
        private string _fileName;

        public string ValidationMessage
        {
            get
            {
                TryGetTargetPath(out _, out string? errorMessage);
                return errorMessage ?? string.Empty;
            }
        }

        public bool CanApply
        {
            get
            {
                return TryGetTargetPath(out string targetPath, out _)
                    && !string.Equals(originalPath, targetPath, StringComparison.Ordinal);
            }
        }

        public RenameFileViewModel()
            : this(Path.Combine(Environment.CurrentDirectory, "library.bib"))
        {
        }

        public RenameFileViewModel(string fullPath)
        {
            originalPath = Path.GetFullPath(fullPath);
            DirectoryPath = Path.GetDirectoryName(originalPath) ?? string.Empty;
            OriginalFileName = Path.GetFileName(originalPath);
            _fileName = OriginalFileName;
            BaseNameLength = Path.GetFileNameWithoutExtension(OriginalFileName).Length;
        }

        partial void OnFileNameChanged(string value)
        {
            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(CanApply));
        }

        public bool TryGetTargetPath(out string targetPath, out string? errorMessage)
        {
            targetPath = string.Empty;
            errorMessage = null;

            string fileName = (FileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                errorMessage = I18n.Get("Rename.Validation.EmptyFileName");
                return false;
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                errorMessage = I18n.Get("Rename.Validation.InvalidFileName");
                return false;
            }

            string extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
            {
                fileName += ".bib";
            }
            else if (!extension.Equals(".bib", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = I18n.Get("Rename.Validation.MustUseBib");
                return false;
            }

            targetPath = Path.GetFullPath(Path.Combine(DirectoryPath, fileName));
            if (!string.Equals(DirectoryPath, Path.GetDirectoryName(targetPath), StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = I18n.Get("Rename.Validation.InvalidFileName");
                return false;
            }

            if (!IsSamePath(originalPath, targetPath)
                && (File.Exists(targetPath) || Directory.Exists(targetPath)))
            {
                errorMessage = I18n.Get("Rename.Validation.TargetExists");
                return false;
            }

            return true;
        }

        private static bool IsSamePath(string left, string right)
        {
            try
            {
                return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
