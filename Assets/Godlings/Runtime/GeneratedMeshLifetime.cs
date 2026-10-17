using UnityEngine;

namespace Godlings
{
    /// <summary>Releases only meshes generated for temporary test geometry.</summary>
    public sealed class GeneratedMeshLifetime : MonoBehaviour
    {
        private void OnDestroy()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
        }
    }
}
