﻿// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers to manage the status bar for the Visual Studio shell.
    /// </summary>
    [Export(typeof(IStatusbarService))]
    public class StatusbarHelper : IStatusbarService
    {
        private readonly Lazy<IVsStatusbar> _statusbar;

        private IVsStatusbar Statusbar => _statusbar.Value;

        public static IStatusbarService Default => GoogleCloudExtensionPackage.Instance.StatusbarHelper;

        [ImportingConstructor]
        public StatusbarHelper(Lazy<SVsServiceProvider> serviceProvider)
        {
            _statusbar = new Lazy<IVsStatusbar>(
                () =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return (IVsStatusbar)serviceProvider.Value.GetService(typeof(SVsStatusbar));
                });
        }

        /// <summary>
        /// Change the text in the status bar. If the status bar is frozen no change is made.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public void SetText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                SetTextImpl(text);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    GoogleCloudExtensionPackage.Instance.GetService<SVsActivityLog, IVsActivityLog>();
                vsActivityLog.LogError($"Failed to write to the status bar: {ex.Message}");
            }
        }

        private void SetTextImpl(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Statusbar.IsFrozen(out int frozen);
            if (!Convert.ToBoolean(frozen))
            {
                Statusbar.SetText(text);
            }
        }

        /// <summary>
        /// Change the text in the status bar. If the status bar is frozen no change is made.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public async Task SetTextAsync(string text)
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                SetTextImpl(text);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    await GoogleCloudExtensionPackage.Instance.GetServiceAsync<SVsActivityLog, IVsActivityLog>();
                await vsActivityLog.LogErrorAsync($"Failed to write to the status bar: {ex.Message}");
            }
        }

        /// <summary>
        /// Change the text in the status bar. If the status bar is frozen no change is made.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public IDisposable FreezeText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                return FreezeTextImpl(text);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    GoogleCloudExtensionPackage.Instance.GetService<SVsActivityLog, IVsActivityLog>();
                vsActivityLog.LogError($"Failed to write to the status bar: {ex.Message}");
                return new Disposable();
            }
        }

        /// <summary>
        /// Change the text in the status bar. If the status bar is frozen no change is made.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public async Task<IDisposable> FreezeTextAsync(string text)
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                return FreezeTextImpl(text);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    await GoogleCloudExtensionPackage.Instance.GetServiceAsync<SVsActivityLog, IVsActivityLog>();
                vsActivityLog.LogError($"Failed to write to the status bar: {ex.Message}");
                return new Disposable();
            }
        }

        private IDisposable FreezeTextImpl(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Statusbar.IsFrozen(out int frozen);
            if (!Convert.ToBoolean(frozen))
            {
                Statusbar.GetText(out string existingText);
                Statusbar.SetText(text);
                Statusbar.FreezeOutput(Convert.ToInt32(true));

                void UnfreezeText()
                {
                    Statusbar.FreezeOutput(Convert.ToInt32(false));
                    Statusbar.SetText(existingText);
                }

                return new Disposable(UnfreezeText);
            }

            return new Disposable();
        }

        /// <summary>
        /// Shows an animation to show that a deploy action is being executed. This animation will only show
        /// if VS is showing all of the visual effects. The result of the method should stored in a variable in a
        /// using statement.
        /// </summary>
        /// <returns>An implementation of <seealso cref="IDisposable"/> that will stop the animation on dispose.</returns>
        public IDisposable ShowDeployAnimation()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(1, ref animation);
                return new Disposable(HideDeployAnimation);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    GoogleCloudExtensionPackage.Instance.GetService<SVsActivityLog, IVsActivityLog>();
                vsActivityLog.LogError($"Failed to show animation: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Shows an animation to show that a deploy action is being executed. This animation will only show
        /// if VS is showing all of the visual effects. The result of the method should stored in a variable in a
        /// using statement.
        /// </summary>
        /// <returns>An implementation of <seealso cref="IDisposable"/> that will stop the animation on dispose.</returns>
        public async Task<IDisposable> ShowDeployAnimationAsync()
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            return ShowDeployAnimation();
        }

        /// <summary>
        /// Shows the progress bar indicator in the Visual Studio shell.
        /// </summary>
        /// <param name="label">The label to use for the progress indicator.</param>
        /// <returns>
        /// An instance of <seealso cref="ProgressBarHelper"/> which can be used to both update the progress bar
        /// and perform cleanup.
        /// </returns>
        public IDisposableProgress ShowProgressBar(string label)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return new ProgressBarHelper(Statusbar, label);
        }

        /// <summary>
        /// Shows the progress bar indicator in the Visual Studio shell.
        /// </summary>
        /// <param name="label">The label to use for the progress indicator.</param>
        /// <returns>
        /// An instance of <seealso cref="ProgressBarHelper"/> which can be used to both update the progress bar
        /// and perform cleanup.
        /// </returns>
        public async Task<IDisposableProgress> ShowProgressBarAsync(string label)
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            return ShowProgressBar(label);
        }

        private void HideDeployAnimation()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(0, ref animation);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    GoogleCloudExtensionPackage.Instance.GetService<SVsActivityLog, IVsActivityLog>();
                vsActivityLog.LogError($"Failed to hide animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Freezes the status bar, which prevents updates from other parts of the VS shell.
        /// </summary>
        /// <returns>An implementation of <seealso cref="IDisposable"/> that will unfreeze the status bar on dispose.</returns>
        public IDisposable Freeze()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Statusbar.FreezeOutput(1);
                return new Disposable(UnFreeze);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    GoogleCloudExtensionPackage.Instance.GetService<SVsActivityLog, IVsActivityLog>();
                vsActivityLog.LogError($"Failed to freeze the status bar output: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Freezes the status bar, which prevents updates from other parts of the VS shell.
        /// </summary>
        /// <returns>An implementation of <seealso cref="IDisposable"/> that will unfreeze the status bar on dispose.</returns>
        public async Task<IDisposable> FreezeAsync()
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            return Freeze();

        }

        private void UnFreeze()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Statusbar.FreezeOutput(0);
            }
            catch (Exception ex)
            {
                IVsActivityLog vsActivityLog =
                    GoogleCloudExtensionPackage.Instance.GetService<SVsActivityLog, IVsActivityLog>();
                vsActivityLog.LogError($"Failed to unfreeze the status bar output: {ex.Message}");
            }
        }
    }
}
