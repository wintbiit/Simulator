using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class Unfold : EditorWindow
    {

        [MenuItem("Gq_Tools/Unfold")]
        public static void ShowWin()
        {
            CreateInstance<Unfold>().Show();
        }
        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(10);
            if (GUILayout.Button("Planar-X unfold"))//UI上画一个按钮
            {
                //MonoBehaviour.print("do");
                Unfold0("X");
            }
            if (GUILayout.Button("Planar-Y unfold"))//UI上画一个按钮
            {
                //MonoBehaviour.print("do");
                Unfold0("Y");
            }
            if (GUILayout.Button("Planar-Z unfold"))//UI上画一个按钮
            {
                //MonoBehaviour.print("do");
                Unfold0("Z");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Cubic unfold"))//UI上画一个按钮
            {
                //MonoBehaviour.print("do");
                Unfold1();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("unfold by lightmap UV"))//UI上画一个按钮
            {
                //MonoBehaviour.print("do");
                Unfold2();
            }
            GUILayout.EndHorizontal();
        }
        //Planar Unfold
        static void Unfold0(string inStr)
        {
            var objs = Selection.objects;
            if (inStr == "X")
            {
                foreach (var obj in objs)//for每个选中的物体
                {
                    var go = obj as GameObject;
                    if (!(go is null))
                    {
                        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                        var vertices = mesh.vertices;
                        var uvs = new Vector2[vertices.Length];

                        //Planar Unfold
                        for (int i = 0; i < uvs.Length; i++)
                        {
                            uvs[i] = new Vector2( vertices[i].y, vertices[i].z);
                        }
                        mesh.uv = uvs;
                    }
                }
            }
            if (inStr == "Y")
            {
                foreach (var obj in objs)//for每个选中的物体
                {
                    var go = obj as GameObject;
                    if (!(go is null))
                    {
                        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                        var vertices = mesh.vertices;
                        var uvs = new Vector2[vertices.Length];

                        //Planar Unfold
                        for (var i = 0; i < uvs.Length; i++)
                        {
                            uvs[i] = new Vector2(vertices[i].x,vertices[i].z);
                        }
                        mesh.uv = uvs;
                    }
                }
            }
            if (inStr == "Z")
            {
                foreach (var obj in objs)//for每个选中的物体
                {
                    var go = obj as GameObject;
                    if (!(go is null))
                    {
                        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                        var vertices = mesh.vertices;
                        var uvs = new Vector2[vertices.Length];

                        //Planar Unfold
                        for (var i = 0; i < uvs.Length; i++)
                        {
                            uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
                        }
                        mesh.uv = uvs;
                    }
                }
            }
        }
        //Cubic Unfold
        private static void Unfold1()
        {
            var objs = Selection.objects;
            foreach (var obj in objs)//for每个选中的物体
            {
                var go = obj as GameObject;
                if (!(go is null))
                {
                    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                    var vertices = mesh.vertices;
                    var uvs = new Vector2[vertices.Length];
                    var normals = mesh.normals;

                    //Cubic Unfold
                    for (var i = 0; i < normals.Length; i++)
                    {
                        //X-Plane
                        if (Mathf.Abs(normals[i].x) > Mathf.Abs(normals[i].y) && Mathf.Abs(normals[i].x) > Mathf.Abs(normals[i].z))
                        {
                            uvs[i] = new Vector2(vertices[i].y, vertices[i].z);
                        }
                        //Y-Plane
                        if (Mathf.Abs(normals[i].y) > Mathf.Abs(normals[i].x) && Mathf.Abs(normals[i].y) > Mathf.Abs(normals[i].z))
                        {
                            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
                        }
                        //Z-Plane
                        if (Mathf.Abs(normals[i].z) > Mathf.Abs(normals[i].x) && Mathf.Abs(normals[i].z) > Mathf.Abs(normals[i].y))
                        {
                            uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
                        }
                    }
                    mesh.uv = uvs;
                }
            }
        }
        //use lightmap UV
        private static void Unfold2()
        {
            var objs = Selection.objects;
            foreach (var obj in objs)//对于每个选中的物体
            {
                var go = obj as GameObject;
                if (!(go is null))
                {
                    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                    mesh.uv = mesh.uv2;
                }
            }
        }
    }
}