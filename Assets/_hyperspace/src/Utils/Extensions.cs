using Hyperspace.Entities;
using Photon.Bolt;

namespace Hyperspace.Utils
{
    public static class Extensions
    {
        public static Player GetPlayer (this BoltConnection connection)
        {
            return (Player)connection.UserData;
        }
    }
}