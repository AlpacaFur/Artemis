﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ninject;
using Ninject.Parameters;
using ReactiveUI;

namespace Artemis.UI.Shared.Services.Builders
{
    public class ContentDialogBuilder
    {
        private readonly ContentDialog _contentDialog;
        private readonly IKernel _kernel;
        private readonly Window _parent;
        private ContentDialogViewModelBase? _viewModel;

        internal ContentDialogBuilder(IKernel kernel, Window parent)
        {
            _kernel = kernel;
            _parent = parent;
            _contentDialog = new ContentDialog
            {
                CloseButtonText = "Close"
            };
        }

        public ContentDialogBuilder WithTitle(string? title)
        {
            _contentDialog.Title = title;
            return this;
        }

        public ContentDialogBuilder WithContent(string? content)
        {
            _contentDialog.Content = content;
            return this;
        }

        public ContentDialogBuilder WithDefaultButton(ContentDialogButton defaultButton)
        {
            _contentDialog.DefaultButton = defaultButton;
            return this;
        }

        public ContentDialogBuilder HavingPrimaryButton(Action<ContentDialogButtonBuilder> configure)
        {
            ContentDialogButtonBuilder builder = new();
            configure(builder);

            _contentDialog.IsPrimaryButtonEnabled = true;
            _contentDialog.PrimaryButtonText = builder.Text;
            _contentDialog.PrimaryButtonCommand = builder.Command;
            _contentDialog.PrimaryButtonCommandParameter = builder.CommandParameter;

            // I feel like this isn't my responsibility...
            if (builder.Command != null)
            {
                _contentDialog.IsPrimaryButtonEnabled = builder.Command.CanExecute(builder.CommandParameter);
                builder.Command.CanExecuteChanged += (_, _) => _contentDialog.IsPrimaryButtonEnabled = builder.Command.CanExecute(builder.CommandParameter);
            }

            return this;
        }

        public ContentDialogBuilder HavingSecondaryButton(Action<ContentDialogButtonBuilder> configure)
        {
            ContentDialogButtonBuilder builder = new();
            configure(builder);

            _contentDialog.IsSecondaryButtonEnabled = true;
            _contentDialog.SecondaryButtonText = builder.Text;
            _contentDialog.SecondaryButtonCommand = builder.Command;
            _contentDialog.SecondaryButtonCommandParameter = builder.CommandParameter;

            // I feel like this isn't my responsibility...
            if (builder.Command != null)
            {
                _contentDialog.IsSecondaryButtonEnabled = builder.Command.CanExecute(builder.CommandParameter);
                builder.Command.CanExecuteChanged += (_, _) => _contentDialog.IsSecondaryButtonEnabled = builder.Command.CanExecute(builder.CommandParameter);
            }

            return this;
        }

        public ContentDialogBuilder WithCloseButtonText(string? text)
        {
            _contentDialog.CloseButtonText = text;
            return this;
        }

        public ContentDialogBuilder WithViewModel<T>(out T viewModel, params (string name, object? value)[] parameters) where T : ContentDialogViewModelBase
        {
            IParameter[] paramsArray = parameters.Select(kv => new ConstructorArgument(kv.name, kv.value)).Cast<IParameter>().ToArray();
            viewModel = _kernel.Get<T>(paramsArray);
            viewModel.ContentDialog = _contentDialog;
            _contentDialog.Content = viewModel;

            _viewModel = viewModel;
            return this;
        }

        public async Task<ContentDialogResult> ShowAsync()
        {
            if (_parent.Content is not Panel panel)
                throw new ArtemisSharedUIException($"The parent window {_parent.GetType().FullName} should contain a panel at its root");

            try
            {
                panel.Children.Add(_contentDialog);
                ContentDialogResult result = await _contentDialog.ShowAsync();
                
                // Take the dialog away from the VM in case it's going to try to hide it again or whatever...
                if (_viewModel != null) 
                    _viewModel.ContentDialog = null;

                return result;
            }
            finally
            {
                panel.Children.Remove(_contentDialog);
            }
        }
    }

    public class ContentDialogButtonBuilder
    {
        internal ContentDialogButtonBuilder()
        {
        }

        internal string? Text { get; set; }
        internal ICommand? Command { get; set; }
        internal object? CommandParameter { get; set; }

        public ContentDialogButtonBuilder WithText(string? text)
        {
            Text = text;
            return this;
        }

        public ContentDialogButtonBuilder WithCommand(ICommand? command)
        {
            Command = command;
            return this;
        }

        public ContentDialogButtonBuilder WithCommandParameter(object? commandParameter)
        {
            CommandParameter = commandParameter;
            return this;
        }
    }
}