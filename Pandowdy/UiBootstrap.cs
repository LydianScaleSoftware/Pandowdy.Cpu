// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Microsoft.Extensions.Hosting;

namespace Pandowdy
{
    public static class UiBootstrap
    {
        public static void IntegrateUiServiceProvider(IHost host)
        {
            DesktopServiceProvider.SetProvider(host.Services);
        }
    }
}
