using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Ringline.Art.Editor
{
    // Art-owned assets only. Never searches, edits or saves a gameplay scene.
    public static class M0LookDevBuilder
    {
        const string Root = "Assets/RinglineArt/M0";
        const string Evidence = "Docs/Art/Evidence";
        const float Width = 1.5f, Height = 3.6f, Thickness = .22f;

        [MenuItem("Ringline/Art/Generate M0 LookDev Samples")]
        public static void Generate()
        {
            // Preview objects are created with DontSave flags before they are moved,
            // following Unity's own PreviewRenderUtility construction pattern.
            var previous = SceneManager.GetActiveScene();
            bool wasDirty = previous.IsValid() && previous.isDirty;
            int previousRoots = previous.IsValid() ? previous.rootCount : 0;
            int previousPreviews = EditorSceneManager.previewSceneCount;
            GenerateAssets();
            if (previous.IsValid() && (previous.isDirty != wasDirty || previous.rootCount != previousRoots))
                throw new InvalidOperationException("Previously active scene state changed during art generation.");
            if (EditorSceneManager.previewSceneCount != previousPreviews)
                throw new InvalidOperationException("Art generation leaked a preview scene.");
            File.AppendAllText(Evidence + "/M0_Validation.txt", "\nPASS: previously active scene root count and dirty flag preserved; no preview scene leak from generation.");
        }

        static void SaveOwnAssets()
        {
            foreach(var guid in AssetDatabase.FindAssets("", new[]{Root}))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if(asset != null) AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        static void GenerateAssets()
        {
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Textures");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Evidence);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit is required; no shader fallback is accepted.");
            var wall = SaveMesh("SM_Wall_150x360x022", MakeBox(Width, Height, Thickness, 0, true));
            var unitMesh = Object.Instantiate(wall);
            unitMesh.vertices = wall.vertices.Select(v => new Vector3(v.x/Width, v.y/Height-.5f, v.z/Thickness)).ToArray();
            unitMesh.RecalculateBounds(); unitMesh.RecalculateNormals();
            var unitWall = SaveMesh("SM_Wall_UnitCentered", unitMesh);
            var floor = SaveMesh("SM_Floor_150x025x150", MakeBox(Width, .25f, Width, -.25f, false));
            var clean = MakeMaterial("M_Wall_Basic_Point", shader, MakeTexture("T_Wall_Basic", false, false));
            var agedTex = MakeTexture("T_Wall_Weathered", true, false);
            var aged = MakeMaterial("M_Wall_Weathered_Point", shader, agedTex);
            var bilinearTex = MakeTexture("T_Wall_Weathered_Bilinear", true, false, FilterMode.Bilinear);
            MakeMaterial("M_Wall_Weathered_Bilinear", shader, bilinearTex);
            var floorMat = MakeMaterial("M_Floor_Point", shader, MakeTexture("T_Floor", true, true));
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var basicPrefab = MakePrefab(scene, "PF_Wall_Basic_150", wall, clean);
                var agedPrefab = MakePrefab(scene, "PF_Wall_Weathered_150", wall, aged);
                MakePrefab(scene, "PF_Floor_150", floor, floorMat);
                MakePrefab(scene, "PF_Wall_Basic_UnitCentered", unitWall, clean);
                MakePrefab(scene, "PF_Wall_Weathered_UnitCentered", unitWall, aged);
                var report = Validate(basicPrefab, agedPrefab, wall, floor);
                var comparison = NewObject(scene, "M0_Comparison");
                var basicView = RenderObject(scene, "Basic", wall, clean, new Vector3(-1.65f, 0, 0), comparison.transform);
                var agedView = RenderObject(scene, "Weathered", wall, aged, new Vector3(.3f, 0, 0), comparison.transform);
                var bilinear = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_Wall_Weathered_Bilinear.mat");
                var bilinearView = RenderObject(scene, "Weathered_Bilinear", wall, bilinear, new Vector3(2.25f, 0, 0), comparison.transform);
                for (int x = -3; x <= 3; x++)
                    for (int z = -2; z <= 2; z++)
                        RenderObject(scene, "Floor", floor, floorMat, new Vector3(x * Width, 0, z * Width), comparison.transform);
                var light = NewObject(scene, "LookDev_Sun").AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, .9f, .73f);
                light.intensity = 1.8f;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(48, -32, 0);
                var fill = NewObject(scene, "LookDev_Fill").AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = new Color(.65f, .79f, 1f);
                fill.intensity = .5f;
                fill.transform.rotation = Quaternion.Euler(35, 145, 0);
                fill.shadows = LightShadows.None;
                var camera = NewObject(scene, "LookDev_Camera").AddComponent<Camera>();
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.16f, .22f, .23f);
                camera.orthographic = true;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 100;
                Capture(camera, "M0_FilterComparison.png", new Vector3(7, 6, -10), new Vector3(.3f, 1.2f, 0), 3.2f);
                bilinearView.SetActive(false);
                basicView.transform.localPosition = new Vector3(-.95f, 0, 0);
                agedView.transform.localPosition = new Vector3(.95f, 0, 0);
                Capture(camera, "M0_Close.png", new Vector3(6, 5, -8), new Vector3(0, 1.4f, 0), 3f);
                Capture(camera, "M0_Diorama.png", new Vector3(10, 13, -14), Vector3.zero, 7f);
                Capture(camera, "M0_PlayerView.png", new Vector3(8, 11, -12), new Vector3(0, 1, 0), 6.5f);
                Capture(camera, "M0_Overview.png", new Vector3(.3f, 18, -.01f), new Vector3(.3f, 0, 0), 6f);
                report += "\nrendering: " + SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName;
                report += "\nUnity: " + Application.unityVersion;
                report += "\nPipeline: " + GraphicsSettings.currentRenderPipeline?.GetType().FullName;
                File.WriteAllText(Evidence + "/M0_Validation.txt", report);
                Debug.Log("M0_ART_VALIDATION_PASS\n" + report);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            SaveOwnAssets();
            AssetDatabase.Refresh();
        }

        static GameObject NewObject(Scene scene, string name)
        {
            var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.hideFlags = HideFlags.None;
            return go;
        }

        static Mesh SaveMesh(string name, Mesh mesh)
        {
            mesh.name = name;
            string path = Root + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                // Set mesh channels through the Mesh API so live GPU vertex buffers
                // are invalidated as well as the serialized asset data.
                existing.Clear();
                existing.vertices = mesh.vertices; existing.normals = mesh.normals;
                existing.uv = mesh.uv; existing.triangles = mesh.triangles;
                existing.bounds = mesh.bounds;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh); return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Mesh MakeBox(float width, float height, float depth, float bottom, bool wall)
        {
            float x = width / 2, z = depth / 2, y = bottom, top = bottom + height;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect rect)
            {
                int start = vertices.Count;
                vertices.AddRange(new[] {a, b, c, d});
                float atlasWidth = 64f, atlasHeight = wall ? 128f : 64f;
                uvs.AddRange(new[] {new Vector2(rect.xMin/atlasWidth, rect.yMin/atlasHeight), new Vector2(rect.xMin/atlasWidth, rect.yMax/atlasHeight), new Vector2(rect.xMax/atlasWidth, rect.yMax/atlasHeight), new Vector2(rect.xMax/atlasWidth, rect.yMin/atlasHeight)});
                indices.AddRange(new[] {start, start+1, start+2, start, start+2, start+3});
            }
            Rect front = wall ? new Rect(4, 4, 32, 77) : new Rect(4, 4, 32, 5);
            Rect edge = wall ? new Rect(44, 4, 5, 77) : front;
            Rect cap = wall ? new Rect(4, 96, 32, 5) : new Rect(4, 4, 32, 32);
            Face(new Vector3(-x,y,-z), new Vector3(-x,top,-z), new Vector3(x,top,-z), new Vector3(x,y,-z), front);
            Face(new Vector3(x,y,z), new Vector3(x,top,z), new Vector3(-x,top,z), new Vector3(-x,y,z), front);
            Face(new Vector3(-x,y,z), new Vector3(-x,top,z), new Vector3(-x,top,-z), new Vector3(-x,y,-z), edge);
            Face(new Vector3(x,y,-z), new Vector3(x,top,-z), new Vector3(x,top,z), new Vector3(x,y,z), edge);
            Face(new Vector3(-x,top,-z), new Vector3(-x,top,z), new Vector3(x,top,z), new Vector3(x,top,-z), cap);
            Face(new Vector3(-x,y,z), new Vector3(-x,y,-z), new Vector3(x,y,-z), new Vector3(x,y,z), cap);
            var mesh = new Mesh();
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static Texture2D MakeTexture(string name, bool weathered, bool floor, FilterMode filter = FilterMode.Point)
        {
            // Approved photographic source -> actual low-res reduction -> masked
            // retouch -> atlas. Never overwrite this with a procedural pixel pattern.
            string sourcePath = "ArtSource/Ringline/M0/FinalAtlases/" + name + ".png";
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("Run M0PhotoTexturePipeline Prepare and Complete first.", sourcePath);
            string path = Root + "/Textures/" + name + ".png";
            File.Copy(sourcePath, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = filter;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.mipMapBias = 0;
            importer.anisoLevel = 1;
            importer.maxTextureSize = 128;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material MakeMaterial(string name, Shader shader, Texture texture)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", texture); mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0); mat.SetFloat("_Smoothness", .05f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject RenderObject(Scene scene, string name, Mesh mesh, Material material, Vector3 position, Transform parent = null)
        {
            var go = NewObject(scene, name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        static GameObject MakePrefab(Scene scene, string name, Mesh mesh, Material material)
        {
            var root = NewObject(scene, name);
            RenderObject(scene, "Mesh", mesh, material, Vector3.zero, root.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static string Validate(GameObject basic, GameObject aged, Mesh wall, Mesh floor)
        {
            void Check(bool ok, string detail) { if (!ok) throw new InvalidOperationException("M0 art validation: " + detail); }
            foreach(var prefab in new[]{basic, aged, AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PF_Floor_150.prefab")})
            {
                Check(prefab != null, "missing prefab");
                Check(prefab.GetComponentsInChildren<Component>(true).All(c => c is Transform || c is MeshFilter || c is MeshRenderer), "visual prefab contains non-render component");
                Check(prefab.transform.localScale == Vector3.one && prefab.transform.localPosition == Vector3.zero && prefab.transform.localRotation == Quaternion.identity, "root TRS");
                foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                    Check(renderer.sharedMaterial.shader.name == "Universal Render Pipeline/Lit" && renderer.sharedMaterial.shader.isSupported, "unsupported material");
            }
            Check(wall.vertexCount == 24 && wall.triangles.Length/3 == 12, "wall topology");
            Check((wall.bounds.size-new Vector3(Width,Height,Thickness)).sqrMagnitude < 1e-8f, "wall bounds");
            Check(Mathf.Abs(wall.bounds.min.y)<1e-6f && Mathf.Abs(floor.bounds.max.y)<1e-6f, "floor contact pivot");
            foreach(var mesh in new[]{wall, floor})
            {
                Check(mesh.subMeshCount==1 && mesh.uv.Length==mesh.vertexCount, "submesh/UV count");
                Check(mesh.uv.All(uv=>uv.x>=0 && uv.x<=1 && uv.y>=0 && uv.y<=1), "UV atlas bounds");
            }
            foreach(var name in new[]{"T_Wall_Basic","T_Wall_Weathered","T_Wall_Weathered_Bilinear","T_Floor"})
            {
                string path=Root+"/Textures/"+name+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int expectedHeight=name=="T_Floor" ? 64 : 128;
                Check(texture.width==64 && texture.height==expectedHeight, "texture resolution");
                Check(importer.mipmapEnabled && importer.sRGBTexture && !importer.isReadable && importer.wrapMode==TextureWrapMode.Clamp && importer.textureCompression==TextureImporterCompression.Uncompressed, "texture import settings");
                Check(importer.filterMode==(name.EndsWith("Bilinear") ? FilterMode.Bilinear : FilterMode.Point), "texture filter");
            }
            var fixture = EditorSceneManager.NewPreviewScene();
            try
            {
                var anchor = NewObject(fixture,"StructuralAnchor_TEST_ONLY");
                var collision = NewObject(fixture,"Collision"); collision.transform.SetParent(anchor.transform,false);
                var collider = collision.AddComponent<BoxCollider>();
                collider.size = new Vector3(Width,Height,Thickness); collider.center = Vector3.up*Height/2;
                var visual = NewObject(fixture,"Visual"); visual.transform.SetParent(anchor.transform,false);
                int colliderId = collider.GetInstanceID();
                foreach(var prefab in new[]{basic,aged})
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,fixture);
                    instance.transform.SetParent(visual.transform,false);
                    Check(collider.GetInstanceID()==colliderId && collider.size==new Vector3(Width,Height,Thickness) && collision.transform.localPosition==Vector3.zero, "collision changed across swap");
                    Check(instance.GetComponentsInChildren<Collider>().Length==0, "visual adds collision");
                    Check(instance.transform.localScale==Vector3.one, "swap changes scale");
                    Object.DestroyImmediate(instance);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(fixture); }
            foreach(var name in new[]{"Basic","Weathered"})
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PF_Wall_"+name+"_UnitCentered.prefab");
                Check(prefab.GetComponentsInChildren<Component>(true).All(c=>c is Transform || c is MeshFilter || c is MeshRenderer), "adapter contains non-render component");
                var bounds = prefab.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
                Check(bounds.size==Vector3.one && bounds.center.sqrMagnitude<1e-8f, "unit-centered adapter bounds");
                Check(prefab.transform.localScale==Vector3.one, "adapter scale");
            }
            return "PASS: 5 render-only visual prefabs; identity root TRS; supported URP Lit; wall 24 vertices/12 triangles; wall bounds 1.5x3.6x0.22m; floor top y=0; 2 unit-centered adapter meshes; UV count/range; 4 texture resolution/import/filter checks; synthetic anchor collider identity/size unchanged across basic-weathered swap.\nNOT TESTED: real station anchors, NavMesh, walkability, doors, moving camera shimmer, 90-degree occlusion, FBX/GLB round trip, target hardware performance.\nClose/PlayerView: Basic Point / Weathered Point. FilterComparison: Basic Point / Weathered Point / Weathered Bilinear.\nPhotographic wall surface 32x77 texels in 64x128 atlas; floor 32x32 texels in 64x64 atlas.\nNo gameplay scene opened, modified or saved. Preview scenes closed after capture.";
        }

        static void Capture(Camera camera, string name, Vector3 position, Vector3 target, float size)
        {
            camera.transform.position = position; camera.transform.LookAt(target); camera.orthographicSize = size;
            var rt = new RenderTexture(1400, 900, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var tex = new Texture2D(1400,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0,0,1400,900),0,0); tex.Apply();
                File.WriteAllBytes(Evidence+"/"+name,tex.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active=previous; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); }
        }
    }
}
