using Avalonia.Controls;
using Avalonia.Controls.Templates;
using NoiseMachineDotNet.ViewModels;
using System;

namespace NoiseMachineDotNet
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object data)
        {
            if (data is null)
            {
                return null;
            }

            string name = data.GetType().FullName!.Replace("ViewModel", "View");
            Type? type = Type.GetType(name);

            return type != null ? (Control)Activator.CreateInstance(type)! : new TextBlock { Text = name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}