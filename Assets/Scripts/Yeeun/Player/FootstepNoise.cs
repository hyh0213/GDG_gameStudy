using System;
using UnityEngine;

namespace Yeeun.Player
{
    // AI listeners filter by source scene and distance. Radius is in world metres.
    public static class FootstepNoise
    {
        public static event Action<MazePlayerController, Vector3, float, bool> Emitted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Emitted = null;
        public static void Emit(MazePlayerController source, Vector3 position, float radius, bool running)
            => Emitted?.Invoke(source, position, radius, running);
    }
}
