using UnityEngine;

namespace Surfer;

/// <summary>
/// Base class for all Surfer MonoBehaviours.
/// Suppresses GC finalization to prevent IL2CPP ClassInjector.Finalize crashes
/// when native Unity handles are destroyed during scene changes.
///
/// IMPORTANT: SuppressFinalize must run in Awake (object creation), NOT OnDestroy.
/// In IL2CPP, the GC schedules finalizers early — by the time OnDestroy fires,
/// SuppressFinalize may be too late to cancel an already-scheduled finalization.
/// </summary>
public abstract class SurferBehaviour : MonoBehaviour
{
    protected virtual void Awake()
    {
        System.GC.SuppressFinalize(this);
    }

    protected virtual void OnDestroy()
    {
        System.GC.SuppressFinalize(this);
    }
}
