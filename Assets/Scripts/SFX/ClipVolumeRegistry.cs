using System.Collections.Generic;
using UnityEngine;

// shared clip-based volume registry
//  - sliders register clip groups with a normalized multiplier
//  - audio callers can query the final multiplier for any clip
//  - multiple sliders affecting the same clip stack multiplicatively
public static class ClipVolumeRegistry
{
    private sealed class Registration
    {
        public string Id;
        public float VolumeMultiplier;
    }

    private static readonly Dictionary<AudioClip, Dictionary<string, Registration>> ClipRegistrations =
        new Dictionary<AudioClip, Dictionary<string, Registration>>();

    // adds or updates one slider registration across a set of clips
    public static void RegisterClips(string registrationId, IReadOnlyList<AudioClip> clips, float volumeMultiplier)
    {
        if (string.IsNullOrWhiteSpace(registrationId) || clips == null)
        {
            return;
        }

        UnregisterClips(registrationId);

        var clampedMultiplier = Mathf.Clamp01(volumeMultiplier);
        for (var i = 0; i < clips.Count; i++)
        {
            var clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            if (!ClipRegistrations.TryGetValue(clip, out var registrations))
            {
                registrations = new Dictionary<string, Registration>();
                ClipRegistrations[clip] = registrations;
            }

            registrations[registrationId] = new Registration
            {
                Id = registrationId,
                VolumeMultiplier = clampedMultiplier
            };
        }
    }

    // removes one slider registration from every clip it previously controlled
    public static void UnregisterClips(string registrationId)
    {
        if (string.IsNullOrWhiteSpace(registrationId))
        {
            return;
        }

        var clipsToRemove = ListPool<AudioClip>.Get();

        foreach (var pair in ClipRegistrations)
        {
            pair.Value.Remove(registrationId);
            if (pair.Value.Count == 0)
            {
                clipsToRemove.Add(pair.Key);
            }
        }

        for (var i = 0; i < clipsToRemove.Count; i++)
        {
            ClipRegistrations.Remove(clipsToRemove[i]);
        }

        ListPool<AudioClip>.Release(clipsToRemove);
    }

    // applies all registered clip multipliers to a local effect volume
    public static float ScaleVolume(AudioClip clip, float baseVolume)
    {
        return Mathf.Clamp01(baseVolume) * GetMultiplier(clip);
    }

    // returns the combined multiplier for a clip
    public static float GetMultiplier(AudioClip clip)
    {
        if (clip == null || !ClipRegistrations.TryGetValue(clip, out var registrations))
        {
            return 1f;
        }

        var multiplier = 1f;
        foreach (var registration in registrations.Values)
        {
            multiplier *= registration.VolumeMultiplier;
        }

        return Mathf.Clamp01(multiplier);
    }

    // tiny pooled list helper to avoid repeated temp allocations
    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>();
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}
