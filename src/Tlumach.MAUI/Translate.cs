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
}
