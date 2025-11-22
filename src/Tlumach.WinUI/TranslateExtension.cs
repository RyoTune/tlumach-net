// <copyright file="TranslateExtension.cs" company="Allied Bits Ltd.">
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

using System;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;

namespace Tlumach.WinUI
{
    [MarkupExtensionReturnType(ReturnType = typeof(Binding))]
    [ContentProperty(Name = nameof(Unit))]
    public sealed class TranslateExtension : MarkupExtension
    {
        private readonly XamlTranslateCore _core;
        private readonly DispatcherQueue _dq;

        // Accept either a TranslationUnit or a Binding (WinUI has Binding, not BindingBase)
        public object? Unit { get; set; }

        public TranslateExtension()
        {
            _dq = DispatcherQueue.GetForCurrentThread()
                 ?? throw new InvalidOperationException("TranslateExtension must be created on the UI thread.");

            _core = new XamlTranslateCore(a =>
            {
                if (_dq.HasThreadAccess)
                    a();
                else
                    _dq.TryEnqueue(() => a()); // wrap Action into DispatcherQueueHandler
                                               // or: _dq.TryEnqueue(DispatcherQueuePriority.Normal, () => a());
            });
        }

        // Listener to host the incoming binding
        private sealed class BindingListener : DependencyObject
        {
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register(
                    nameof(Value),
                    typeof(object),
                    typeof(BindingListener),
                    new PropertyMetadata(defaultValue: null, OnChanged));

            public object? Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }

#pragma warning disable MA0046 // The delegate must have 2 parameters
            public event Action<object?>? Changed;
#pragma warning restore MA0046 // The delegate must have 2 parameters

            private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                => ((BindingListener)d).Changed?.Invoke(e.NewValue);
        }

        private BindingListener? _listener;

        protected override object ProvideValue()
        {
            if (Unit is Binding unitBinding)
            {
                _listener ??= new BindingListener();
                _listener.Changed -= OnUnitChanged; // avoid duplicates
                _listener.Changed += OnUnitChanged;

                // Attach the user's binding so updates flow into the listener
                BindingOperations.SetBinding(_listener, BindingListener.ValueProperty, unitBinding);

                // Initialize core.Unit from current listener value
                _core.Unit = _listener.Value as Tlumach.TranslationUnit;
            }
            else
            {
                _core.Unit = Unit as Tlumach.TranslationUnit;
            }

            // Return a live binding to Value
            return new Binding
            {
                Source = _core,
                Path = new PropertyPath(nameof(XamlTranslateCore.Value)),
                Mode = BindingMode.OneWay,
            };
        }

        private void OnUnitChanged(object? value)
            => _core.Unit = value as Tlumach.TranslationUnit;
    }
}
