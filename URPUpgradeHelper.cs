using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// URP 升级辅助工具
/// 提供预检查、详细报告等功能
/// </summary>
public class URPUpgradeHelper : EditorWindow
{
    private Vector2 scrollPos;
    private List<MaterialInfo> materialsToUpgrade = new List<MaterialInfo>();
    private List<string> warnings = new List<string>();
    private bool scanned = false;
    
    private class MaterialInfo
    {
        public Material material;
        public string path;
        public string currentShader;
        public string targetShader;
        public bool canUpgrade;
    }
    
    [MenuItem("Tools/URP 升级助手/预检查工具", false, 101)]
    public static void ShowWindow()
    {
        var window = GetWindow<URPUpgradeHelper>("URP 预检查");
        window.minSize = new Vector2(500, 400);
    }
    
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🔍 URP 升级预检查工具", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("在升级前检查项目中需要处理的内容", EditorStyles.miniLabel);
        
        EditorGUILayout.Space(15);
        
        // 扫描按钮
        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("📊 扫描项目", GUILayout.Height(35)))
        {
            ScanProject();
        }
        GUI.backgroundColor = Color.white;
        
        if (scanned)
        {
            EditorGUILayout.Space(10);
            
            // 显示警告
            if (warnings.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("⚠️ 警告 (" + warnings.Count + ")", EditorStyles.boldLabel);
                
                foreach (var warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(10);
            
            // 显示材质列表
            DrawMaterialList();
            
            EditorGUILayout.Space(10);
            
            // 显示其他统计
            DrawStatistics();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void ScanProject()
    {
        materialsToUpgrade.Clear();
        warnings.Clear();
        
        EditorUtility.DisplayProgressBar("扫描项目", "正在扫描材质...", 0f);
        
        try
        {
            // 扫描所有材质
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            
            for (int i = 0; i < materialGuids.Length; i++)
            {
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("扫描项目", 
                        $"正在扫描材质 ({i}/{materialGuids.Length})...", 
                        (float)i / materialGuids.Length);
                }
                
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                
                // 跳过 Packages
                if (path.StartsWith("Packages/")) continue;
                
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                
                var info = AnalyzeMaterial(mat, path);
                if (info != null)
                {
                    materialsToUpgrade.Add(info);
                }
            }
            
            // 检查场景中的对象
            CheckSceneObjects();
            
            // 检查设置
            CheckProjectSettings();
            
            scanned = true;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
    
    private MaterialInfo AnalyzeMaterial(Material mat, string path)
    {
        string shaderName = mat.shader.name;
        
        // 已经是 URP
        if (shaderName.StartsWith("Universal Render Pipeline") ||
            shaderName.StartsWith("URP") ||
            shaderName.StartsWith("Shader Graphs"))
        {
            return null;
        }
        
        // UI 和 Skybox 不需要处理
        if (shaderName.Contains("UI/") || shaderName.Contains("Skybox/"))
        {
            return null;
        }
        
        var info = new MaterialInfo
        {
            material = mat,
            path = path,
            currentShader = shaderName,
            canUpgrade = true
        };
        
        // 确定目标着色器
        if (shaderName == "Standard" || shaderName == "Standard (Specular setup)")
        {
            info.targetShader = "Universal Render Pipeline/Lit";
        }
        else if (shaderName.Contains("Unlit"))
        {
            info.targetShader = "Universal Render Pipeline/Unlit";
        }
        else if (shaderName.Contains("Particles"))
        {
            info.targetShader = "Universal Render Pipeline/Particles/Lit";
        }
        else if (shaderName.StartsWith("Legacy") || shaderName.StartsWith("Mobile"))
        {
            info.targetShader = "Universal Render Pipeline/Lit";
        }
        else if (shaderName.StartsWith("Hidden/") || shaderName.StartsWith("Internal"))
        {
            info.canUpgrade = false;
            info.targetShader = "(系统着色器，跳过)";
        }
        else
        {
            // 自定义着色器
            info.canUpgrade = false;
            info.targetShader = "(需要手动处理)";
            warnings.Add($"自定义着色器需要手动处理: {mat.name} ({shaderName})");
        }
        
        return info;
    }
    
    private void CheckSceneObjects()
    {
        // 检查相机
        var cameras = Object.FindObjectsOfType<Camera>(true);
        int camerasWithoutURPData = 0;
        
        var urpCameraDataType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        
        if (urpCameraDataType != null)
        {
            foreach (var cam in cameras)
            {
                if (cam.GetComponent(urpCameraDataType) == null)
                    camerasWithoutURPData++;
            }
        }
        
        if (camerasWithoutURPData > 0)
        {
            warnings.Add($"场景中有 {camerasWithoutURPData} 个相机需要添加 URP Camera Data");
        }
        
        // 检查灯光
        var lights = Object.FindObjectsOfType<Light>(true);
        int lightsWithoutURPData = 0;
        
        var urpLightDataType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalLightData, Unity.RenderPipelines.Universal.Runtime");
        
        if (urpLightDataType != null)
        {
            foreach (var light in lights)
            {
                if (light.GetComponent(urpLightDataType) == null)
                    lightsWithoutURPData++;
            }
        }
        
        if (lightsWithoutURPData > 0)
        {
            warnings.Add($"场景中有 {lightsWithoutURPData} 个灯光需要添加 URP Light Data");
        }
    }
    
    private void CheckProjectSettings()
    {
        // 检查当前渲染管线
        var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (currentRP == null)
        {
            warnings.Add("Graphics Settings 中未设置渲染管线");
        }
        
        // 检查 Color Space
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            warnings.Add("建议将 Color Space 设置为 Linear (当前为 Gamma)");
        }
    }
    
    private void DrawMaterialList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        int upgradeCount = 0;
        int manualCount = 0;
        
        foreach (var info in materialsToUpgrade)
        {
            if (info.canUpgrade) upgradeCount++;
            else manualCount++;
        }
        
        EditorGUILayout.LabelField($"📦 材质分析 (可自动升级: {upgradeCount}, 需手动处理: {manualCount})", 
            EditorStyles.boldLabel);
        
        if (materialsToUpgrade.Count == 0)
        {
            EditorGUILayout.HelpBox("没有需要升级的材质，或所有材质已经是 URP 格式。", MessageType.Info);
        }
        else
        {
            // 显示前 20 个
            int shown = 0;
            foreach (var info in materialsToUpgrade)
            {
                if (shown >= 20) break;
                
                EditorGUILayout.BeginHorizontal();
                
                // 状态图标
                string icon = info.canUpgrade ? "✅" : "⚠️";
                EditorGUILayout.LabelField(icon, GUILayout.Width(20));
                
                // 材质名
                if (GUILayout.Button(info.material.name, EditorStyles.linkLabel, GUILayout.Width(150)))
                {
                    Selection.activeObject = info.material;
                    EditorGUIUtility.PingObject(info.material);
                }
                
                // 着色器转换
                EditorGUILayout.LabelField($"{info.currentShader} → {info.targetShader}", 
                    EditorStyles.miniLabel);
                
                EditorGUILayout.EndHorizontal();
                shown++;
            }
            
            if (materialsToUpgrade.Count > 20)
            {
                EditorGUILayout.LabelField($"... 还有 {materialsToUpgrade.Count - 20} 个材质", 
                    EditorStyles.miniLabel);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawStatistics()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📈 统计信息", EditorStyles.boldLabel);
        
        var cameras = Object.FindObjectsOfType<Camera>(true);
        var lights = Object.FindObjectsOfType<Light>(true);
        var renderers = Object.FindObjectsOfType<Renderer>(true);
        
        EditorGUILayout.LabelField($"场景中的相机: {cameras.Length}");
        EditorGUILayout.LabelField($"场景中的灯光: {lights.Length}");
        EditorGUILayout.LabelField($"场景中的渲染器: {renderers.Length}");
        EditorGUILayout.LabelField($"项目中需处理的材质: {materialsToUpgrade.Count}");
        
        EditorGUILayout.EndVertical();
    }
}

/// <summary>
/// 材质着色器批量替换工具
/// </summary>
public class ShaderReplacementTool : EditorWindow
{
    private Shader sourceShader;
    private Shader targetShader;
    private Vector2 scrollPos;
    private List<Material> foundMaterials = new List<Material>();
    
    [MenuItem("Tools/URP 升级助手/着色器批量替换", false, 102)]
    public static void ShowWindow()
    {
        var window = GetWindow<ShaderReplacementTool>("着色器替换");
        window.minSize = new Vector2(400, 300);
    }
    
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🔄 着色器批量替换", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(10);
        
        sourceShader = (Shader)EditorGUILayout.ObjectField("源着色器", sourceShader, typeof(Shader), false);
        targetShader = (Shader)EditorGUILayout.ObjectField("目标着色器", targetShader, typeof(Shader), false);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("查找材质"))
        {
            FindMaterials();
        }
        
        GUI.enabled = foundMaterials.Count > 0 && targetShader != null;
        GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
        if (GUILayout.Button("替换着色器"))
        {
            ReplaceShaders();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        if (foundMaterials.Count > 0)
        {
            EditorGUILayout.LabelField($"找到 {foundMaterials.Count} 个使用此着色器的材质:", EditorStyles.boldLabel);
            
            foreach (var mat in foundMaterials)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button(mat.name, EditorStyles.linkLabel))
                {
                    Selection.activeObject = mat;
                    EditorGUIUtility.PingObject(mat);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void FindMaterials()
    {
        foundMaterials.Clear();
        
        if (sourceShader == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择源着色器", "确定");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader == sourceShader)
            {
                foundMaterials.Add(mat);
            }
        }
    }
    
    private void ReplaceShaders()
    {
        if (targetShader == null) return;
        
        int count = 0;
        
        foreach (var mat in foundMaterials)
        {
            Undo.RecordObject(mat, "Replace Shader");
            mat.shader = targetShader;
            EditorUtility.SetDirty(mat);
            count++;
        }
        
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("完成", $"已替换 {count} 个材质的着色器", "确定");
        
        foundMaterials.Clear();
    }
}

/// <summary>
/// 常用 URP 着色器快速访问
/// </summary>
public static class URPShaderQuickAccess
{
    [MenuItem("Tools/URP 升级助手/快速设置/所有材质 → URP Lit")]
    public static void AllToURPLit()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("错误", "找不到 URP/Lit 着色器，请确保已安装 URP", "确定");
            return;
        }
        
        if (!EditorUtility.DisplayDialog("确认", 
            "这将把所有 Standard 材质转换为 URP/Lit\n\n是否继续？", "继续", "取消"))
            return;
        
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && (mat.shader.name == "Standard" || mat.shader.name == "Standard (Specular setup)"))
            {
                mat.shader = urpLit;
                EditorUtility.SetDirty(mat);
                count++;
            }
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已转换 {count} 个材质", "确定");
    }
    
    [MenuItem("Tools/URP 升级助手/快速设置/修复粉红色材质")]
    public static void FixPinkMaterials()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("错误", "找不到 URP/Lit 着色器", "确定");
            return;
        }
        
        int count = 0;
        var renderers = Object.FindObjectsOfType<Renderer>(true);
        
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader.name == "Hidden/InternalErrorShader")
                {
                    mat.shader = urpLit;
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"修复了 {count} 个粉红色材质", "确定");
    }
    
    [MenuItem("Tools/URP 升级助手/快速设置/打开 Graphics Settings")]
    public static void OpenGraphicsSettings()
    {
        SettingsService.OpenProjectSettings("Project/Graphics");
    }
    
    [MenuItem("Tools/URP 升级助手/快速设置/打开 Quality Settings")]
    public static void OpenQualitySettings()
    {
        SettingsService.OpenProjectSettings("Project/Quality");
    }
}
