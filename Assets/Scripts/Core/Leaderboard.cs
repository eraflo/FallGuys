using System.Collections.Generic;
using System.Linq;

namespace FallGuys.Core
{
    /// <summary>
    /// Entry in the leaderboard representing a player's finish.
    /// </summary>
    [System.Serializable]
    public struct LeaderboardEntry
    {
        public ulong ClientId;
        public string PlayerName;
        public float FinishTime;
        public int Rank;

        public LeaderboardEntry(ulong clientId, string playerName, float finishTime, int rank = 0)
        {
            ClientId = clientId;
            PlayerName = playerName;
            FinishTime = finishTime;
            Rank = rank;
        }
    }

    /// <summary>
    /// Tracks player finish times and provides ranked results.
    /// </summary>
    public class Leaderboard
    {
        private List<LeaderboardEntry> _entries = new List<LeaderboardEntry>();

        /// <summary>
        /// Records a player finishing the race.
        /// </summary>
        public void RecordFinish(ulong clientId, string playerName, float finishTime)
        {
            // Don't record duplicates
            if (_entries.Any(e => e.ClientId == clientId)) return;

            _entries.Add(new LeaderboardEntry(clientId, playerName, finishTime));
        }

        /// <summary>
        /// Gets all entries sorted by finish time (best first), with ranks assigned.
        /// </summary>
        public List<LeaderboardEntry> GetRankedEntries()
        {
            var sorted = _entries.OrderBy(e => e.FinishTime).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                entry.Rank = i + 1;
                sorted[i] = entry;
            }

            return sorted;
        }

        /// <summary>
        /// Checks if a player has already finished.
        /// </summary>
        public bool HasFinished(ulong clientId)
        {
            return _entries.Any(e => e.ClientId == clientId);
        }

        /// <summary>
        /// Gets the number of players who have finished.
        /// </summary>
        public int FinishedCount => _entries.Count;

        /// <summary>
        /// Clears all entries (for a new game).
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}
