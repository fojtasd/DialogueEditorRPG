using UnityEngine;

namespace Contracts.Contracts {
    [CreateAssetMenu(fileName = "IntelSO", menuName = "ScriptableObjects/IntelSO")]
    public class IntelSO : ScriptableObject {
        [field: SerializeField] public string ID {get; set;}
        [field: SerializeField] public string Name {get; set;}
        [field: SerializeField] public string Description {get; set;}
    }
}