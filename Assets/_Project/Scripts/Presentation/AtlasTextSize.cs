using TMPro;
using UnityEngine;

namespace ProjectAtlas.Presentation
{
    public sealed class AtlasTextSize : MonoBehaviour
    {
        public float baseSize;
        public void Apply(float scale) { GetComponent<TMP_Text>().fontSize = baseSize * scale; }
    }
}
