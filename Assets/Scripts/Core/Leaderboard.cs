using System.Collections.Generic;
using FallGuys.Networking;

namespace FallGuys.Core
{
    public class Leaderboard
    {
        // Using ClientId as key for robustness in networking
        public Dictionary<ulong, int> PlayerScores = new Dictionary<ulong, int>();
    }
}
