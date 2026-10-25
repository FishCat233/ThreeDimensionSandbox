using UnityEngine;

namespace TDS
{
    public readonly struct HitInfo
    {
        public HitInfo(MeleeFighter attacker, MeleeFighter victim, Vector3 point)
        {
            Attacker = attacker;
            Victim = victim;
            Point = point;
        }

        public MeleeFighter Attacker { get; }

        public MeleeFighter Victim { get; }

        public Vector3 Point { get; }
    }
}
