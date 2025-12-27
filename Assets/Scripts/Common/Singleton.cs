using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

// This is heavily based on the implementation here:
// https://gamedev.stackexchange.com/a/151547 - Cosmic Giant

namespace Common {
    public abstract class Singleton<T> : Singleton where T : MonoBehaviour {
        static T _instance;
        static bool _isInitialising;
        static readonly object _InstanceLock = new();
        static event Action<T> OnInstanceAvailable;

        public static T Instance {
            get {
                lock (_InstanceLock) {
                    // do nothing if currently quitting
                    if (IsQuitting)
                        return _instance;

                    // instance already found?
                    if (_instance != null)
                        return _instance;

                    _isInitialising = true;

                    // search for any in-scene instance of T
                    var allInstances = FindObjectsByType<T>(FindObjectsSortMode.None);

                    switch (allInstances.Length) {
                        // found exactly one?
                        case 1:
                            _instance = allInstances[0]; // found none?
                            break;
                        case 0:
                            _instance = new GameObject($"Singleton<{typeof(T)}>").AddComponent<T>(); // multiple found?
                            break;
                        default: {
                            _instance = allInstances[0];

                            // destroy the duplicates
                            for (int index = 1; index < allInstances.Length; ++index) {
                                Debug.LogError(
                                    $"Destroying duplicate {typeof(T)} on {allInstances[0].gameObject.name}");
                                Destroy(allInstances[index].gameObject);
                            }

                            break;
                        }
                    }

                    _isInitialising = false;
                    // notify any awaiters that the instance is now available
                    OnInstanceAvailable?.Invoke(_instance);
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Attempts to get the current instance without creating a new one.
        /// Returns true if an instance already exists.
        /// </summary>
        public static bool TryGetInstance(out T existingInstance) {
            lock (_InstanceLock) {
                existingInstance = _instance;
                return existingInstance != null;
            }
        }

        /// <summary>
        /// Await until an existing instance becomes available, optionally timing out.
        /// This will NOT create a new instance unless <paramref name="createIfMissing"/> is true.
        /// </summary>
        /// <param name="timeoutSeconds">Max seconds to wait; pass <0 to wait indefinitely.</param>
        /// <param name="cancellationToken">External cancellation token.</param>
        /// <param name="createIfMissing">If true and no instance exists, this will create one by accessing <see cref="Instance"/>.</param>
        public static async Task<T> WaitForInstanceAsync(float timeoutSeconds = -1f, CancellationToken cancellationToken = default, bool createIfMissing = false) {
            // early-out: if already quitting, respect cancellation semantics
            if (IsQuitting)
                throw new OperationCanceledException("Application is quitting.");

            // try fast-paths inside the lock
            lock (_InstanceLock) {
                if (_instance != null)
                    return _instance;

                // try to resolve any existing in-scene instance WITHOUT creating a new object
                var AllInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
                switch (AllInstances.Length) {
                    case 1:
                        _instance = AllInstances[0];
                        OnInstanceAvailable?.Invoke(_instance);
                        return _instance;
                    case > 1: {
                        _instance = AllInstances[0];
                        for (int Index = 1; Index < AllInstances.Length; ++Index) {
                            Debug.LogError($"Destroying duplicate {typeof(T)} on {AllInstances[0].gameObject.name}");
                            Destroy(AllInstances[Index].gameObject);
                        }

                        OnInstanceAvailable?.Invoke(_instance);
                        return _instance;
                    }
                    case 0:
                        // no instances found in-scene; optionally create
                        if (createIfMissing)
                            return Instance; // will create and trigger OnInstanceAvailable
                        break;
                }
            }

            // wait asynchronously for Awake() to construct and announce the instance
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(T inst) {
                tcs.TrySetResult(inst);
            }

            OnInstanceAvailable += Handler;

            // safety: check again in case it became available between releasing the lock and subscribing
            lock (_InstanceLock) {
                if (_instance != null)
                    tcs.TrySetResult(_instance);
            }

            CancellationTokenSource timeoutCts = null;
            CancellationTokenRegistration externalCancelReg = default;
            CancellationTokenRegistration timeoutCancelReg = default;
            try {
                if (cancellationToken.CanBeCanceled)
                    externalCancelReg = cancellationToken.Register(() => tcs.TrySetCanceled());

                if (timeoutSeconds >= 0f) {
                    timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                    timeoutCancelReg = timeoutCts.Token.Register(() => tcs.TrySetCanceled());
                }

                return await tcs.Task.ConfigureAwait(false);
            }
            finally {
                OnInstanceAvailable -= Handler;
                externalCancelReg.Dispose();
                timeoutCancelReg.Dispose();
                timeoutCts?.Dispose();
            }
        }

        static void ConstructIfNeeded(Singleton<T> InInstance) {
            lock (_InstanceLock) {
                // only construct if the instance is null and is not being initialised
                if (_instance == null && !_isInitialising) {
                    _instance = InInstance as T;
                    // notify any awaiters
                    OnInstanceAvailable?.Invoke(_instance);
                }
                else if (_instance != null && !_isInitialising) {
                    Debug.LogError($"Destroying duplicate {typeof(T)} on {InInstance.gameObject.name}");
                    Destroy(InInstance.gameObject);
                }
            }
        }

        private void Awake() {
            ConstructIfNeeded(this);

            OnAwake();
        }

        protected virtual void OnAwake()
        {
        }
    }

    public abstract class Singleton : MonoBehaviour {
        protected static bool IsQuitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad() {
            IsQuitting = false;
        }

        private void OnApplicationQuit() {
            IsQuitting = true;
        }
    }
}