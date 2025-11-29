// <copyright file="PlaceholderValueNeededEventArgs.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using Tlumach.Base;

namespace Tlumach
{
    /// <summary>
    /// Contains the arguments of the ParameterValueNEeded event.
    /// </summary>
    public class PlaceholderValueNeededEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the placeholder whose value is requested.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the index of the placeholder whose value is requested. May be -1.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets or sets the value for the placeholder.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the indicator telling the unit that the value should be cached until further notification.
        /// </summary>
        public bool CacheValue { get; set; }

        public PlaceholderValueNeededEventArgs(string name, int index)
        {
            Name = name;
            Index = index;
        }
    }
}
