// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Microsoft.VisualStudio.RazorExtension.Behaviors
{
    public static class ItemSelectedBehavior
    {
        public static DependencyProperty ItemSelectedProperty =
            DependencyProperty.RegisterAttached(nameof(Selector.SelectedItem),
                typeof(ICommand),
                typeof(ItemSelectedBehavior),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(ItemSelectedChanged)));

        public static ICommand GetItemSelected(DependencyObject target)
        {
            return (ICommand)target.GetValue(ItemSelectedProperty);
        }

        public static void SetItemSelected(DependencyObject target, ICommand value)
        {
            target.SetValue(ItemSelectedProperty, value);
        }

        private static void ItemSelectedChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var element = target as Selector;
            if (element != null)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                {
                    element.SelectionChanged += Selector_SelectionChanged;
                }

                else if ((e.NewValue == null) && (e.OldValue != null))
                {
                    element.SelectionChanged -= Selector_SelectionChanged;
                }
            }
        }

        private static void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var element = sender as Selector;
            if (element != null)
            {
                ICommand command = (ICommand)GetItemSelected(element);
                command.Execute(element.SelectedItem);
            }
        }
    }
}
