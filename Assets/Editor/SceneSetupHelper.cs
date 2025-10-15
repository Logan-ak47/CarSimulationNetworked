using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CarSim.Editor
{
    public static class SceneSetupHelper
    {
        [MenuItem("CarSim/Setup/Create Server Scene")]
        public static void CreateServerScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Environment
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);

            // Car placeholder
            GameObject car = new GameObject("Car");
            car.transform.position = new Vector3(0, 1, 0);

            Rigidbody rb = car.AddComponent<Rigidbody>();
            rb.mass = 1300;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(car.transform);
            body.transform.localPosition = new Vector3(0, 0.3f, 0);
            body.transform.localScale = new Vector3(2, 0.5f, 4);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            BoxCollider carCollider = car.AddComponent<BoxCollider>();
            carCollider.center = new Vector3(0, 0.3f, 0);
            carCollider.size = new Vector3(2, 0.5f, 4);

            // CoM
            GameObject com = new GameObject("CenterOfMass");
            com.transform.SetParent(car.transform);
            com.transform.localPosition = new Vector3(0, -0.2f, 0);

            // Wheels
            CreateWheelCollider(car.transform, "WheelCollider_FL", new Vector3(-0.8f, 0, 1.2f), true);
            CreateWheelCollider(car.transform, "WheelCollider_FR", new Vector3(0.8f, 0, 1.2f), true);
            CreateWheelCollider(car.transform, "WheelCollider_RL", new Vector3(-0.8f, 0, -1.2f), false);
            CreateWheelCollider(car.transform, "WheelCollider_RR", new Vector3(0.8f, 0, -1.2f), false);

            CreateWheelMesh(car.transform, "WheelMesh_FL", new Vector3(-0.8f, 0, 1.2f));
            CreateWheelMesh(car.transform, "WheelMesh_FR", new Vector3(0.8f, 0, 1.2f));
            CreateWheelMesh(car.transform, "WheelMesh_RL", new Vector3(-0.8f, 0, -1.2f));
            CreateWheelMesh(car.transform, "WheelMesh_RR", new Vector3(0.8f, 0, -1.2f));

            // Camera anchors
            CreateAnchor(car.transform, "Anchor_Dashboard", new Vector3(0, 0.8f, 1));
            CreateAnchor(car.transform, "Anchor_FL_Wheel", new Vector3(-0.8f, 0.2f, 1.2f));
            CreateAnchor(car.transform, "Anchor_FR_Wheel", new Vector3(0.8f, 0.2f, 1.2f));
            CreateAnchor(car.transform, "Anchor_RL_Wheel", new Vector3(-0.8f, 0.2f, -1.2f));
            CreateAnchor(car.transform, "Anchor_RR_Wheel", new Vector3(0.8f, 0.2f, -1.2f));
            CreateAnchor(car.transform, "Anchor_Engine", new Vector3(0, 0.5f, 1.5f));
            CreateAnchor(car.transform, "Anchor_Exhaust", new Vector3(0, 0, -2f));
            CreateAnchor(car.transform, "Anchor_SteeringLinkage", new Vector3(0, 0.2f, 1.5f));
            CreateAnchor(car.transform, "Anchor_BrakeCaliperFront", new Vector3(-0.8f, 0, 1.2f));
            CreateAnchor(car.transform, "Anchor_SuspensionFront", new Vector3(-0.8f, 0.3f, 1.2f));

            // Server Systems
            GameObject serverSystems = new GameObject("ServerSystems");

            Debug.Log("[SceneSetup] Server scene created. Add components manually and assign NetConfig asset.");

            EditorSceneManager.SaveScene(scene, "Assets/Server/Scenes/Server_CarSim.unity");
        }

        [MenuItem("CarSim/Setup/Create Client Scene")]
        public static void CreateClientScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove default camera (UI will have its own camera)
            GameObject mainCam = GameObject.Find("Main Camera");
            if (mainCam != null) Object.DestroyImmediate(mainCam);

            // Client Systems
            GameObject clientSystems = new GameObject("ClientSystems");

            // UI Root (Canvas)
            GameObject uiRoot = new GameObject("UI_Root");
            Canvas canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
            uiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Panel_Connect
            GameObject panelConnect = CreateUIPanel(canvas.transform, "Panel_Connect");
            CreateUIText(panelConnect.transform, "Text_Title", "REMOTE CAR CONTROL", new Vector2(0, 200));
            CreateUIInputField(panelConnect.transform, "Input_ServerIP", "192.168.1.100", new Vector2(0, 100));
            CreateUIInputField(panelConnect.transform, "Input_Token", "demo-token-123456", new Vector2(0, 50));
            CreateUIButton(panelConnect.transform, "Button_Connect", "CONNECT", new Vector2(0, -50));
            CreateUIText(panelConnect.transform, "Text_Status", "Enter server IP and connect", new Vector2(0, -150));

            // Panel_Drive
            GameObject panelDrive = CreateUIPanel(canvas.transform, "Panel_Drive");
            panelDrive.SetActive(false);

            Debug.Log("[SceneSetup] Client scene created. Add UI elements manually for full drive interface.");

            EditorSceneManager.SaveScene(scene, "Assets/Client/Scenes/Client_RemoteControl.unity");
        }

        private static void CreateWheelCollider(Transform parent, string name, Vector3 pos, bool isFront)
        {
            GameObject wheel = new GameObject(name);
            wheel.transform.SetParent(parent);
            wheel.transform.localPosition = pos;

            WheelCollider wc = wheel.AddComponent<WheelCollider>();
            wc.mass = 20;
            wc.radius = 0.4f;
            wc.wheelDampingRate = 0.25f;
            wc.suspensionDistance = 0.2f;
            wc.forceAppPointDistance = 0;

            JointSpring spring = wc.suspensionSpring;
            spring.spring = 35000;
            spring.damper = 4500;
            spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;
        }

        private static void CreateWheelMesh(Transform parent, string name, Vector3 pos)
        {
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = name;
            mesh.transform.SetParent(parent);
            mesh.transform.localPosition = pos;
            mesh.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
            mesh.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
        }

        private static void CreateAnchor(Transform parent, string name, Vector3 pos)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent);
            anchor.transform.localPosition = pos;
        }

        private static GameObject CreateUIPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            UnityEngine.UI.Image img = panel.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            return panel;
        }

        private static GameObject CreateUIText(Transform parent, string name, string text, Vector2 pos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 50);

            UnityEngine.UI.Text txt = obj.AddComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return obj;
        }

        private static GameObject CreateUIInputField(Transform parent, string name, string placeholder, Vector2 pos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 40);

            UnityEngine.UI.Image img = obj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            UnityEngine.UI.InputField inputField = obj.AddComponent<UnityEngine.UI.InputField>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.color = Color.white;
            txt.supportRichText = false;

            inputField.textComponent = txt;
            inputField.text = placeholder;

            return obj;
        }

        private static GameObject CreateUIButton(Transform parent, string name, string text, Vector2 pos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 50);

            UnityEngine.UI.Image img = obj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.2f, 0.6f, 0.2f, 1f);

            UnityEngine.UI.Button btn = obj.AddComponent<UnityEngine.UI.Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return obj;
        }
    }
}
