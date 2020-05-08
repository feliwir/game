﻿using game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace lumos
{
    class GameWindow
    {
        private readonly Sdl2Window m_window;
        private GraphicsDevice m_gd;
        private DisposeCollectorResourceFactory m_factory;
        private bool m_windowResized = true;

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action Resized;
        public event Action<KeyEvent> KeyPressed;

        public uint Width => (uint)m_window.Width;
        public uint Height => (uint)m_window.Height;


        public GameWindow(string title)
        {
            WindowCreateInfo wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 800,
                WindowHeight = 600,
                WindowTitle = title,
            };
            m_window = VeldridStartup.CreateWindow(ref wci);
            m_window.Resized += () =>
            {
                m_windowResized = true;
            };
            m_window.KeyDown += OnKeyDown;
        }

        public void Run()
        {

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: true,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);

#if DEBUG
            options.Debug = true;
#endif
            m_gd = VeldridStartup.CreateGraphicsDevice(m_window, options);
            m_factory = new DisposeCollectorResourceFactory(m_gd.ResourceFactory);
            GraphicsDeviceCreated?.Invoke(m_gd, m_factory, m_gd.MainSwapchain);

            Stopwatch sw = Stopwatch.StartNew();
            double previousElapsed = sw.Elapsed.TotalSeconds;

            while (m_window.Exists)
            {
                double newElapsed = sw.Elapsed.TotalSeconds;
                float deltaSeconds = (float)(newElapsed - previousElapsed);

                InputSnapshot inputSnapshot = m_window.PumpEvents();
                InputTracker.UpdateFrameInput(inputSnapshot);

                if (m_window.Exists)
                {
                    previousElapsed = newElapsed;
                    if (m_windowResized)
                    {
                        m_windowResized = false;
                        m_gd.ResizeMainWindow((uint)m_window.Width, (uint)m_window.Height);
                        Resized?.Invoke();
                    }

                    Rendering?.Invoke(deltaSeconds);
                }
            }

            m_gd.WaitForIdle();
            m_factory.DisposeCollector.DisposeAll();
            m_gd.Dispose();
            GraphicsDeviceDestroyed?.Invoke();
        }

        protected void OnKeyDown(KeyEvent keyEvent)
        {
            KeyPressed?.Invoke(keyEvent);
        }
    }
}
