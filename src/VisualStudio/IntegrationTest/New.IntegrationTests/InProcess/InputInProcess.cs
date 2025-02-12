﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;
using Microsoft.VisualStudio.Threading;
using WindowsInput;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class InputInProcess
    {
        internal Task SendAsync(InputKey key, CancellationToken cancellationToken)
            => SendAsync(new InputKey[] { key }, cancellationToken);

        internal Task SendAsync(InputKey[] keys, CancellationToken cancellationToken)
        {
            return SendAsync(
                simulator =>
                {
                    foreach (var key in keys)
                    {
                        key.Apply(simulator);
                    }
                }, cancellationToken);
        }

        internal async Task SendAsync(Action<IInputSimulator> callback, CancellationToken cancellationToken)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await TestServices.Editor.ActivateAsync(cancellationToken);
            });

            callback(new InputSimulator());

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await WaitForApplicationIdleAsync(cancellationToken);
            });
        }

        internal Task SendWithoutActivateAsync(InputKey key, CancellationToken cancellationToken)
            => SendWithoutActivateAsync(new[] { key }, cancellationToken);

        internal Task SendWithoutActivateAsync(InputKey[] keys, CancellationToken cancellationToken)
        {
            return SendWithoutActivateAsync(
                simulator =>
                {
                    foreach (var key in keys)
                    {
                        key.Apply(simulator);
                    }
                }, cancellationToken);
        }

        internal async Task SendWithoutActivateAsync(Action<IInputSimulator> callback, CancellationToken cancellationToken)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            callback(new InputSimulator());

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await WaitForApplicationIdleAsync(cancellationToken);
            });
        }

        internal async Task SendToNavigateToAsync(params InputKey[] keys)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            // Take no direct action regarding activation, but assert the correct item already has focus
            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await TestServices.JoinableTaskFactory.SwitchToMainThreadAsync();
                var searchBox = Assert.IsAssignableFrom<Control>(Keyboard.FocusedElement);
                // Validate the focused control against the "old" search experience as well as the 
                // all-in-one search experience.
                Assert.Contains(searchBox.Name, new[] { "PART_SearchBox", "SearchBoxControl" });
            });

            var inputSimulator = new InputSimulator();
            foreach (var key in keys)
            {
                key.Apply(inputSimulator);

                // Since the all-in-one search experience populates its results asychronously we need
                // to give it time to update prior to applying the next InputKey otherwise we may apply
                // a Return key meant to select an item before it is in the result set.
                await Task.Delay(1000);
            }

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await WaitForApplicationIdleAsync(CancellationToken.None);
            });
        }

        internal async Task MoveMouseAsync(Point point, CancellationToken cancellationToken)
        {
            var horizontalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            var verticalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            var virtualPoint = new ScaleTransform(65535.0 / horizontalResolution, 65535.0 / verticalResolution).Transform(point);

            await SendAsync(simulator => simulator.Mouse.MoveMouseTo(virtualPoint.X, virtualPoint.Y), cancellationToken);

            // ⚠ The call to GetCursorPos is required for correct behavior.
            var actualPoint = NativeMethods.GetCursorPos();
            Assert.True(Math.Abs(actualPoint.X - point.X) <= 1, $"Expected '{Math.Abs(actualPoint.X - point.X)}' <= '1'. Move to '({point.X}, {point.Y})' produced '({actualPoint.X}, {actualPoint.Y})'.");
            Assert.True(Math.Abs(actualPoint.Y - point.Y) <= 1, $"Expected '{Math.Abs(actualPoint.Y - point.Y)}' <= '1'. Move to '({point.X}, {point.Y})' produced '({actualPoint.X}, {actualPoint.Y})'.");
        }
    }
}
