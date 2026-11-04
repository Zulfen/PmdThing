using Microsoft.Extensions.Logging;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Hardware.Input;
using Serilog;
using System.Reflection;
using Serilog.Configuration;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PmdThing.Core.Hardware.Framework
{
    public abstract class PmdFramework
    {
        public PmdGraphics Graphics;
        public IPmdKeyboard Keyboard;

        public ILogger Logger;

        private long _running = 1;

        protected PmdFramework(PmdGraphics sdlGraphics, IPmdKeyboard keyboard)
        {
            Graphics = sdlGraphics;
            Keyboard = keyboard;

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss} {Level:u4}: {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            using var factory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(serilogLogger);
            });

            Logger = factory.CreateLogger("PmdThing");

            Logger.LogInformation("Starting PmdThing v{version}", GetExecutableVersion()?.ToString());
        }

        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        public static Version? GetExecutableVersion()
        {
            return Assembly.GetExecutingAssembly()
                .GetName()
                .Version;
        }

        // update framework stuff
        public abstract void Init();
        public abstract void Shutdown();
        public abstract void Update();
    }
}
