using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// URP 一键升级工具
/// 从内置渲染管线升级到 Universal Render Pipeline
/// </summary>
public class URPOneClickUpgrade : EditorWindow
{
    private static AddRequest addRequest;
    private static ListRequest listRequest;
    
    private Vector2 scrollPos;
    private bool urpInstalled = false;
    private bool checking = false;
    private string statusMessage = "";
    private MessageType statusType = MessageType.Info;
    
    // 升级选项
    private bool upgradeMaterials = true;
    private bool upgradeCameras = true;
    private bool upgradeLights = true;
    private bool createURPAsset = true;
    private bool configureGraphicsSettings = true;
    private bool upgradeSceneMaterials = true;
    private bool upgradeProjectMaterials = true;
    
    // 升级报告
    private List<string> upgradeLog = new List<string>();
    private int materialsUpgraded = 0;
    private int camerasUpgraded = 0;
    private int lightsUpgraded = 0;
    
    [MenuItem("Tools/URP 一键升级工具", false, 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<URPOneClickUpgrade>("URP 一键升级");
        window.minSize = new Vector2(450, 600);
        window.CheckURPStatus();
    }
    
    private void OnEnable()
    {
        CheckURPStatus();
    }
    
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        // 标题
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🚀 URP 一键升级工具", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("从内置渲染管线升级到 Universal Render Pipeline", EditorStyles.miniLabel);
        
        EditorGUILayout.Space(15);
        
        // 状态显示
        DrawStatusSection();
        
        EditorGUILayout.Space(10);
        
        // URP 安装部分
        DrawURPInstallSection();
        
        EditorGUILayout.Space(10);
        
        // 升级选项
        DrawUpgradeOptionsSection();
        
        EditorGUILayout.Space(10);
        
        // 一键升级按钮
        DrawUpgradeButton();
        
        EditorGUILayout.Space(10);
        
        // 单独功能按钮
        DrawIndividualButtons();
        
        EditorGUILayout.Space(10);
        
        // 升级日志
        DrawUpgradeLog();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawStatusSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("📊 当前状态", EditorStyles.boldLabel);
        
        if (checking)
        {
            EditorGUILayout.HelpBox("正在检测 URP 状态...", MessageType.Info);
        }
        else
        {
            string urpStatus = urpInstalled ? "✅ 已安装" : "❌ 未安装";
            EditorGUILayout.LabelField("URP 包状态:", urpStatus);
            
            // 检查当前渲染管线
            var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            string rpStatus = currentRP != null ? "✅ " + currentRP.name : "❌ 未配置 (使用内置管线)";
            EditorGUILayout.LabelField("当前渲染管线:", rpStatus);
        }
        
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawURPInstallSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("📦 第一步: 安装 URP 包", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = !urpInstalled && !checking;
        if (GUILayout.Button("安装 URP 包", GUILayout.Height(30)))
        {
            InstallURP();
        }
        
        GUI.enabled = true;
        if (GUILayout.Button("刷新状态", GUILayout.Width(80), GUILayout.Height(30)))
        {
            CheckURPStatus();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (urpInstalled)
        {
            EditorGUILayout.HelpBox("URP 包已安装，可以进行升级操作。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("请先安装 URP 包，然后再进行升级。", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawUpgradeOptionsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("⚙️ 升级选项", EditorStyles.boldLabel);
        
        createURPAsset = EditorGUILayout.Toggle("创建 URP Asset", createURPAsset);
        configureGraphicsSettings = EditorGUILayout.Toggle("配置 Graphics Settings", configureGraphicsSettings);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("材质升级:", EditorStyles.miniLabel);
        upgradeMaterials = EditorGUILayout.Toggle("  启用材质升级", upgradeMaterials);
        
        GUI.enabled = upgradeMaterials;
        EditorGUI.indentLevel++;
        upgradeSceneMaterials = EditorGUILayout.Toggle("升级场景中的材质", upgradeSceneMaterials);
        upgradeProjectMaterials = EditorGUILayout.Toggle("升级项目中的材质", upgradeProjectMaterials);
        EditorGUI.indentLevel--;
        GUI.enabled = true;
        
        EditorGUILayout.Space(5);
        upgradeCameras = EditorGUILayout.Toggle("升级相机 (添加 URP Data)", upgradeCameras);
        upgradeLights = EditorGUILayout.Toggle("升级灯光设置", upgradeLights);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawUpgradeButton()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("🎯 执行升级", EditorStyles.boldLabel);
        
        GUI.enabled = urpInstalled;
        
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("⚡ 一键完整升级", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("确认升级",
                "即将执行以下操作:\n\n" +
                (createURPAsset ? "• 创建 URP Asset\n" : "") +
                (configureGraphicsSettings ? "• 配置 Graphics Settings\n" : "") +
                (upgradeMaterials ? "• 升级材质\n" : "") +
                (upgradeCameras ? "• 升级相机\n" : "") +
                (upgradeLights ? "• 升级灯光\n" : "") +
                "\n建议先备份项目！是否继续？",
                "继续升级", "取消"))
            {
                PerformFullUpgrade();
            }
        }
        GUI.backgroundColor = Color.white;
        
        GUI.enabled = true;
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawIndividualButtons()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("🔧 单独执行", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("可以单独执行某一步操作", EditorStyles.miniLabel);
        
        GUI.enabled = urpInstalled;
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建 URP Asset"))
        {
            CreateURPAssetOnly();
        }
        if (GUILayout.Button("配置 Graphics"))
        {
            ConfigureGraphicsSettingsOnly();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("升级场景材质"))
        {
            UpgradeSceneMaterialsOnly();
        }
        if (GUILayout.Button("升级项目材质"))
        {
            UpgradeProjectMaterialsOnly();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("升级相机"))
        {
            UpgradeCamerasOnly();
        }
        if (GUILayout.Button("升级灯光"))
        {
            UpgradeLightsOnly();
        }
        EditorGUILayout.EndHorizontal();
        
        GUI.enabled = true;
        
        EditorGUILayout.Space(5);
        
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("清除所有 URP 设置 (还原为内置管线)"))
        {
            if (EditorUtility.DisplayDialog("确认还原",
                "这将清除 URP 设置，还原为内置渲染管线。\n\n材质不会自动还原，需要手动处理。\n\n是否继续？",
                "继续", "取消"))
            {
                ClearURPSettings();
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawUpgradeLog()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📋 升级日志", EditorStyles.boldLabel);
        if (GUILayout.Button("清空", GUILayout.Width(50)))
        {
            upgradeLog.Clear();
            materialsUpgraded = 0;
            camerasUpgraded = 0;
            lightsUpgraded = 0;
        }
        EditorGUILayout.EndHorizontal();
        
        if (materialsUpgraded > 0 || camerasUpgraded > 0 || lightsUpgraded > 0)
        {
            EditorGUILayout.LabelField($"统计: 材质 {materialsUpgraded} | 相机 {camerasUpgraded} | 灯光 {lightsUpgraded}",
                EditorStyles.miniLabel);
        }
        
        if (upgradeLog.Count > 0)
        {
            EditorGUILayout.BeginVertical(EditorStyles.textArea, GUILayout.Height(150));
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            foreach (var log in upgradeLog.TakeLast(50))
            {
                EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("暂无日志", MessageType.None);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    #region URP Installation
    
    private void CheckURPStatus()
    {
        checking = true;
        listRequest = Client.List();
        EditorApplication.update += CheckListProgress;
    }
    
    private void CheckListProgress()
    {
        if (listRequest.IsCompleted)
        {
            EditorApplication.update -= CheckListProgress;
            checking = false;
            
            if (listRequest.Status == StatusCode.Success)
            {
                urpInstalled = listRequest.Result.Any(p => 
                    p.name == "com.unity.render-pipelines.universal");
                
                if (urpInstalled)
                {
                    var urpPackage = listRequest.Result.First(p => 
                        p.name == "com.unity.render-pipelines.universal");
                    statusMessage = $"URP 版本: {urpPackage.version}";
                    statusType = MessageType.Info;
                }
            }
            else
            {
                statusMessage = "检测 URP 状态失败: " + listRequest.Error.message;
                statusType = MessageType.Error;
            }
            
            Repaint();
        }
    }
    
    private void InstallURP()
    {
        statusMessage = "正在安装 URP...";
        statusType = MessageType.Info;
        
        addRequest = Client.Add("com.unity.render-pipelines.universal");
        EditorApplication.update += CheckAddProgress;
    }
    
    private void CheckAddProgress()
    {
        if (addRequest.IsCompleted)
        {
            EditorApplication.update -= CheckAddProgress;
            
            if (addRequest.Status == StatusCode.Success)
            {
                urpInstalled = true;
                statusMessage = "URP 安装成功！版本: " + addRequest.Result.version;
                statusType = MessageType.Info;
                Log("✅ URP 包安装成功");
            }
            else
            {
                statusMessage = "URP 安装失败: " + addRequest.Error.message;
                statusType = MessageType.Error;
                Log("❌ URP 包安装失败: " + addRequest.Error.message);
            }
            
            Repaint();
        }
    }
    
    #endregion
    
    #region Full Upgrade
    
    private void PerformFullUpgrade()
    {
        upgradeLog.Clear();
        materialsUpgraded = 0;
        camerasUpgraded = 0;
        lightsUpgraded = 0;
        
        Log("========== 开始 URP 升级 ==========");
        
        try
        {
            // 1. 创建 URP Asset
            if (createURPAsset)
            {
                CreateURPAssetOnly();
            }
            
            // 2. 配置 Graphics Settings
            if (configureGraphicsSettings)
            {
                ConfigureGraphicsSettingsOnly();
            }
            
            // 3. 升级材质
            if (upgradeMaterials)
            {
                if (upgradeSceneMaterials)
                    UpgradeSceneMaterialsOnly();
                if (upgradeProjectMaterials)
                    UpgradeProjectMaterialsOnly();
            }
            
            // 4. 升级相机
            if (upgradeCameras)
            {
                UpgradeCamerasOnly();
            }
            
            // 5. 升级灯光
            if (upgradeLights)
            {
                UpgradeLightsOnly();
            }
            
            Log("========== URP 升级完成 ==========");
            Log($"统计: 材质 {materialsUpgraded} | 相机 {camerasUpgraded} | 灯光 {lightsUpgraded}");
            
            statusMessage = "升级完成！请检查升级日志。";
            statusType = MessageType.Info;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("升级完成", 
                $"URP 升级已完成！\n\n" +
                $"• 升级材质: {materialsUpgraded}\n" +
                $"• 升级相机: {camerasUpgraded}\n" +
                $"• 升级灯光: {lightsUpgraded}\n\n" +
                "请检查升级日志了解详情。", "确定");
        }
        catch (System.Exception ex)
        {
            Log("❌ 升级过程出错: " + ex.Message);
            statusMessage = "升级出错: " + ex.Message;
            statusType = MessageType.Error;
        }
    }
    
    #endregion
    
    #region Individual Operations
    
    private void CreateURPAssetOnly()
    {
        Log("--- 创建 URP Asset ---");
        
        // 确保目录存在
        string folderPath = "Assets/Settings/URP";
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Settings", "URP");
        
        // 创建 URP Asset
        var urpAssetType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
        
        if (urpAssetType == null)
        {
            Log("❌ 无法找到 URP Asset 类型，请确保 URP 已正确安装");
            return;
        }
        
        // 使用 ScriptableObject.CreateInstance
        string assetPath = folderPath + "/URP_Asset.asset";
        
        if (File.Exists(Application.dataPath.Replace("Assets", "") + assetPath))
        {
            Log("⚠️ URP Asset 已存在: " + assetPath);
            return;
        }
        
        // 使用菜单命令创建
        try
        {
            // 方法1: 尝试通过反射创建
            var pipelineAsset = ScriptableObject.CreateInstance(urpAssetType);
            if (pipelineAsset != null)
            {
                AssetDatabase.CreateAsset(pipelineAsset, assetPath);
                Log("✅ 创建 URP Asset: " + assetPath);
                
                // 同时创建 Renderer
                CreateURPRenderer(folderPath, pipelineAsset);
            }
        }
        catch (System.Exception ex)
        {
            Log("⚠️ 自动创建失败，请手动创建: " + ex.Message);
            Log("   右键 Assets > Create > Rendering > URP Asset (with Universal Renderer)");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    private void CreateURPRenderer(string folderPath, Object pipelineAsset)
    {
        try
        {
            var rendererType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime");
            
            if (rendererType != null)
            {
                string rendererPath = folderPath + "/URP_Renderer.asset";
                var renderer = ScriptableObject.CreateInstance(rendererType);
                AssetDatabase.CreateAsset(renderer, rendererPath);
                Log("✅ 创建 URP Renderer: " + rendererPath);
                
                // 尝试将 Renderer 添加到 Pipeline Asset
                var rendererListField = pipelineAsset.GetType().GetField("m_RendererDataList", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (rendererListField != null)
                {
                    var rendererArray = System.Array.CreateInstance(rendererType, 1);
                    rendererArray.SetValue(renderer, 0);
                    rendererListField.SetValue(pipelineAsset, rendererArray);
                    EditorUtility.SetDirty(pipelineAsset);
                }
            }
        }
        catch (System.Exception ex)
        {
            Log("⚠️ 创建 Renderer 时出错: " + ex.Message);
        }
    }
    
    private void ConfigureGraphicsSettingsOnly()
    {
        Log("--- 配置 Graphics Settings ---");
        
        // 查找 URP Asset
        var urpAssets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        
        if (urpAssets.Length == 0)
        {
            Log("❌ 未找到 URP Asset，请先创建");
            return;
        }
        
        string assetPath = AssetDatabase.GUIDToAssetPath(urpAssets[0]);
        var pipelineAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>(assetPath);
        
        if (pipelineAsset == null)
        {
            Log("❌ 无法加载 URP Asset");
            return;
        }
        
        // 设置 Graphics Settings
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        Log("✅ 设置 Default Render Pipeline: " + pipelineAsset.name);
        
        // 设置 Quality Settings 中的所有级别
        int qualityLevelCount = QualitySettings.names.Length;
        for (int i = 0; i < qualityLevelCount; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipelineAsset;
            Log($"✅ 设置 Quality Level [{i}] {QualitySettings.names[i]}: {pipelineAsset.name}");
        }
        
        // 恢复到之前的 Quality Level
        QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);
        
        EditorUtility.SetDirty(pipelineAsset);
        AssetDatabase.SaveAssets();
        
        Log("✅ Graphics Settings 配置完成");
    }
    
    private void UpgradeSceneMaterialsOnly()
    {
        Log("--- 升级场景材质 ---");
        
        var renderers = Object.FindObjectsOfType<Renderer>(true);
        HashSet<Material> processedMaterials = new HashSet<Material>();
        
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && !processedMaterials.Contains(mat))
                {
                    processedMaterials.Add(mat);
                    UpgradeMaterial(mat);
                }
            }
        }
        
        Log($"✅ 场景材质升级完成，处理了 {processedMaterials.Count} 个材质");
    }
    
    private void UpgradeProjectMaterialsOnly()
    {
        Log("--- 升级项目材质 ---");
        
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        int count = 0;
        
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 跳过 Packages 文件夹
            if (path.StartsWith("Packages/")) continue;
            
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                if (UpgradeMaterial(mat))
                {
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Log($"✅ 项目材质升级完成，升级了 {count} 个材质");
    }
    
    private bool UpgradeMaterial(Material mat)
    {
        if (mat == null) return false;
        
        string shaderName = mat.shader.name;
        
        // 已经是 URP 材质
        if (shaderName.StartsWith("Universal Render Pipeline") || 
            shaderName.StartsWith("URP") ||
            shaderName.StartsWith("Shader Graphs"))
        {
            return false;
        }
        
        Shader newShader = null;
        
        // Standard -> URP/Lit
        if (shaderName == "Standard" || shaderName == "Standard (Specular setup)")
        {
            newShader = Shader.Find("Universal Render Pipeline/Lit");
        }
        // Unlit 系列
        else if (shaderName.Contains("Unlit"))
        {
            if (shaderName.Contains("Transparent"))
                newShader = Shader.Find("Universal Render Pipeline/Unlit");
            else if (shaderName.Contains("Color"))
                newShader = Shader.Find("Universal Render Pipeline/Unlit");
            else
                newShader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        // Particles
        else if (shaderName.Contains("Particles"))
        {
            if (shaderName.Contains("Additive"))
                newShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            else if (shaderName.Contains("Multiply"))
                newShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            else
                newShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        }
        // Sprites
        else if (shaderName.Contains("Sprites"))
        {
            newShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (newShader == null)
                newShader = Shader.Find("Sprites/Default");
        }
        // UI
        else if (shaderName.Contains("UI"))
        {
            // UI 着色器通常不需要更换
            return false;
        }
        // Skybox
        else if (shaderName.Contains("Skybox"))
        {
            // Skybox 通常保持不变
            return false;
        }
        // 其他内置着色器 -> URP/Lit
        else if (shaderName.StartsWith("Legacy Shaders") || 
                 shaderName.StartsWith("Mobile/") ||
                 shaderName.StartsWith("Nature/"))
        {
            newShader = Shader.Find("Universal Render Pipeline/Lit");
        }
        
        if (newShader != null && newShader != mat.shader)
        {
            // 保存一些属性
            Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
            Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            
            string oldShader = mat.shader.name;
            mat.shader = newShader;
            
            // 恢复属性到 URP
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", mainColor);
            if (mat.HasProperty("_BaseMap") && mainTex != null)
                mat.SetTexture("_BaseMap", mainTex);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_BumpMap") && normalMap != null)
                mat.SetTexture("_BumpMap", normalMap);
            if (mat.HasProperty("_EmissionMap") && emissionMap != null)
                mat.SetTexture("_EmissionMap", emissionMap);
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emissionColor);
            
            Log($"  材质 [{mat.name}]: {oldShader} → {newShader.name}");
            materialsUpgraded++;
            return true;
        }
        
        return false;
    }
    
    private void UpgradeCamerasOnly()
    {
        Log("--- 升级相机 ---");
        
        var cameras = Object.FindObjectsOfType<Camera>(true);
        
        var urpCameraDataType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        
        if (urpCameraDataType == null)
        {
            Log("❌ 无法找到 URP Camera Data 类型");
            return;
        }
        
        foreach (var cam in cameras)
        {
            var existingData = cam.GetComponent(urpCameraDataType);
            if (existingData == null)
            {
                cam.gameObject.AddComponent(urpCameraDataType);
                Log($"  相机 [{cam.name}]: 添加 UniversalAdditionalCameraData");
                camerasUpgraded++;
            }
            
            // 处理一些常见设置
            cam.allowHDR = true;
            cam.allowMSAA = true;
        }
        
        Log($"✅ 相机升级完成，处理了 {camerasUpgraded} 个相机");
    }
    
    private void UpgradeLightsOnly()
    {
        Log("--- 升级灯光 ---");
        
        var lights = Object.FindObjectsOfType<Light>(true);
        
        var urpLightDataType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalLightData, Unity.RenderPipelines.Universal.Runtime");
        
        if (urpLightDataType == null)
        {
            Log("❌ 无法找到 URP Light Data 类型");
            return;
        }
        
        foreach (var light in lights)
        {
            var existingData = light.GetComponent(urpLightDataType);
            if (existingData == null)
            {
                light.gameObject.AddComponent(urpLightDataType);
                Log($"  灯光 [{light.name}]: 添加 UniversalAdditionalLightData");
                lightsUpgraded++;
            }
        }
        
        Log($"✅ 灯光升级完成，处理了 {lightsUpgraded} 个灯光");
    }
    
    private void ClearURPSettings()
    {
        Log("--- 清除 URP 设置 ---");
        
        // 清除 Graphics Settings
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
        Log("✅ 清除 Default Render Pipeline");
        
        // 清除 Quality Settings
        int qualityLevelCount = QualitySettings.names.Length;
        for (int i = 0; i < qualityLevelCount; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = null;
        }
        Log("✅ 清除所有 Quality Level 的渲染管线设置");
        
        AssetDatabase.SaveAssets();
        
        statusMessage = "已还原为内置渲染管线";
        statusType = MessageType.Info;
    }
    
    #endregion
    
    private void Log(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        upgradeLog.Add($"[{timestamp}] {message}");
        Debug.Log("[URP升级] " + message);
        Repaint();
    }
}
