using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Interfaces
{
    public interface ITelemetryProvider
    {
        void TrackEvent(string eventName, Dictionary<string, string> properties);
        void TrackException(Exception ex, Dictionary<string, string> properties);
        void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success);
    }
}
