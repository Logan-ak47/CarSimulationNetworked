using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarSim.Shared
{
    [CreateAssetMenu(fileName = "NetConfig", menuName = "CarSim/NetConfig", order = 1)]
    public class NetConfig : ScriptableObject
    {
        [Header("Authentication")]
        public string token = "demo-token-123456";

        [Header("Network Ports")]
        public int tcpPort = 9000;
        public int udpPortServer = 9001;
        public int udpPortClientListen = 9002;

        [Header("Simulation Rates")]
        public int simTickRate = 50;
        public int inputSendRate = 60;
        public int stateSendRate = 25;

        [Header("Timeouts & Latency")]
        public float inputStaleThresholdMs = 200f;

#if UNITY_EDITOR
        [MenuItem("CarSim/Create NetConfig Asset")]
        private static void CreateNetConfigAsset()
        {
            string path = "Assets/_Shared/Config";
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] dirs = path.Split('/');
                string parent = dirs[0];
                for (int i = 1; i < dirs.Length; i++)
                {
                    string newFolder = parent + "/" + dirs[i];
                    if (!AssetDatabase.IsValidFolder(newFolder))
                    {
                        AssetDatabase.CreateFolder(parent, dirs[i]);
                    }
                    parent = newFolder;
                }
            }

            NetConfig asset = CreateInstance<NetConfig>();
            string assetPath = path + "/NetConfig.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"NetConfig asset created at {assetPath}");
        }
#endif
    }
}
