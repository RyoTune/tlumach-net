// <copyright file="Translate.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using System.Runtime.CompilerServices;

using Tlumach;

namespace Tlumach.MAUI
{
    using System;

    using Microsoft.Maui.Controls;
    
    [ContentProperty(nameof(Unit))]
    [AcceptEmptyServiceProvider]
    public sealed class Translate : BindableObject, IMarkupExtension<BindingBase>
    {
        private readonly XamlTranslateCore _core =
            new XamlTranslateCore(a => MainThread.BeginInvokeOnMainThread(a));

        // Bindable Unit so XAML can do: Unit="{Binding MyTranslationUnit}"
        public static readonly BindableProperty UnitProperty =
            BindableProperty.Create(
                nameof(Unit),
                typeof(TranslationUnit),
                typeof(Translate),
                defaultValue: null,
                propertyChanged: OnUnitChanged);

        public TranslationUnit? Unit
        {
            get => (TranslationUnit?)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        private static void OnUnitChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var ext = (Translate)bindable;
            ext._core.Unit = (TranslationUnit?)newValue;
        }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
            => new Binding(nameof(XamlTranslateCore.Value), source: _core, mode: BindingMode.OneWay);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);
    }

    /*

    [ContentProperty(nameof(Unit))]
    [AcceptEmptyServiceProvider]
    public sealed class Translate : BindableObject, IMarkupExtension<BindingBase>
    {
        public static readonly BindableProperty UnitProperty =
            BindableProperty.Create(nameof(Unit), typeof(TranslationUnit), typeof(Translate), null, propertyChanged: OnUnitChanged);

        public TranslationUnit Unit
        {
            get => (TranslationUnit)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(string), typeof(Translate), string.Empty);

        private static event EventHandler<CultureChangedEventArgs>? OnCultureChanged;

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // Constructor
        public Translate()
        {
            // Subscribe to the external service culture change event.
            // This subscription should be here to ensure it happens for every instance.
            OnCultureChanged += Class_OnCultureChanged;
        }

        // The callback for when the Unit property changes
        private static void OnUnitChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TranslationUnit? currentUnit = oldValue as TranslationUnit;
            TranslationUnit? newUnit = newValue as TranslationUnit;
            if (currentUnit != newValue)
            {
                // Subscribe to the external service culture change event.
                // This subscription should be here to ensure it happens for every instance.
                if (currentUnit?.TranslationManager is not null)
                    currentUnit.TranslationManager.OnCultureChanged -= TranslationProvider_OnCultureChanged;
                if (newUnit?.TranslationManager is not null)
                    newUnit.TranslationManager.OnCultureChanged += TranslationProvider_OnCultureChanged;
            }

            if (bindable is Translate extension && newUnit is not null)
            {
                // Initialize the Value property with the current translated string.
                extension.Value = newUnit.CurrentValue;
            }
        }

        BindingBase IMarkupExtension<BindingBase>.ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding(nameof(Value), source: this);
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding(nameof(Value), source: this);
        }

        // Event handler for the external service
        private static void TranslationProvider_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
#pragma warning disable S4220
            OnCultureChanged?.Invoke(sender, args);
#pragma warning restore S4220
        }

        private void Class_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
            // Update the Value property on the UI thread.
            // This is the key to triggering the binding update.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Unit != null)
                    Value = Unit.CurrentValue;
            });
        }
    }
    */
}
