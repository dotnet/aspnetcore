// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Serves as a base class for components influencing the HTML head tag.
    /// </summary>
    public abstract class HeadElementBase : ComponentBase, IDisposable
    {
        /// <summary>
        /// The <see cref="Extensions.HeadManager"/> enforcing changes to the head tag.
        /// </summary>
        [Inject]
        protected HeadManager HeadManager { get; set; } = default!;

        internal LinkedListNode<HeadElementBase> Node { get; }

        /// <summary>
        /// Gets an object that uniquely identifies the HTML element being modified.
        /// </summary>
        internal abstract object ElementKey { get; }

        /// <summary>
        /// Instantiates a new <see cref="HeadElementBase"/> instance.
        /// </summary>
        protected HeadElementBase()
        {
            Node = new LinkedListNode<HeadElementBase>(this);
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            if (HeadManager == null)
            {
                throw new InvalidOperationException($"{GetType()} requires the {typeof(HeadManager)} service.");
            }
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            HeadManager.NotifyChanged(this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            HeadManager.NotifyDisposed(this);
        }

        internal abstract ValueTask<object> GetInitialStateAsync();

        internal abstract ValueTask ResetInitialStateAsync(object initialState);

        internal abstract ValueTask ApplyAsync();
    }
}
