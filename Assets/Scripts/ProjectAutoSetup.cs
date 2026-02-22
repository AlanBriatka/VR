using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ProjectAutoSetup : MonoBehaviour
{
    [ContextMenu("Auto-Configure Scene")]
    public void ConfigureScene()
    {
        // 1. Setup ObjectPool if missing
        if (ObjectPool.Instance == null)
        {
            GameObject pool = new GameObject("ObjectPool");
            pool.AddComponent<ObjectPool>();
        }

        // 2. Setup ImpactManager
        if (ImpactManager.Instance == null)
        {
            GameObject manager = new GameObject("ImpactManager");
            manager.AddComponent<ImpactManager>();
        }

        // 3. Setup ComboManager
        if (ComboManager.Instance == null)
        {
            GameObject combo = new GameObject("ComboManager");
            combo.AddComponent<ComboManager>();
        }

        // 4. Setup WaveManager
        if (WaveManager.Instance == null)
        {
            GameObject wave = new GameObject("WaveManager");
            wave.AddComponent<WaveManager>();
        }

        // 5. Setup TimeManipulation
        if (TimeManipulation.Instance == null)
        {
            GameObject time = new GameObject("TimeManipulation");
            time.AddComponent<TimeManipulation>();
        }

        Debug.Log("Core systems auto-configured! Please assign Prefabs and Audio in the inspector.");
    }
}
