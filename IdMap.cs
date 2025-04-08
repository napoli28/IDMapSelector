using System.Collections.Generic;
using UnityEngine;

namespace IdMapSelector
{
    public class IdMap
    {
        private readonly Dictionary<int, Component> idMap = new();
        private readonly Dictionary<GameObject, int> objMap = new();

        private Texture2D texture;
        private string idMapTextureName;

        private int max = 1;
        private Queue<int> recycleBin = new();
        private MaterialPropertyBlock mpb;

        public IdMap(string idMapTextureName)
        {
            this.idMapTextureName = idMapTextureName;
            Debug.Log(idMapTextureName);
        }

        private int AllocateId()
        {
            return recycleBin.Count > 0 ? recycleBin.Dequeue() : max++;
        }

        public Color IdToColor(int id)
        {
            var r = id & 255;
            var g = (id >> 8) & 255;
            var b = (id >> 16) & 255;
            return (Color)new Color32((byte)r, (byte)g, (byte)b, 255);
        }
        public int ColorToId(Color color)
        {
            var r = (int)(color.r * 255);
            var g = (int)(color.g * 255);
            var b = (int)(color.b * 255);
            return r | (g << 8) | (b << 16);
        }

        public Color ReadTexture(int x, int y)
        {
            RenderTexture currentActiveRT = RenderTexture.active;
            var rt = Shader.GetGlobalTexture(idMapTextureName) as RenderTexture;
            RenderTexture.active = rt;

            if (texture == null || texture.width != rt.width || texture.height != rt.height)
            {
                texture = new Texture2D(rt.width, rt.height);
            }
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            x = (int)(x * (texture.width / (float)Screen.width));
            y = (int)(y * (texture.height / (float)Screen.height));
            Debug.Log($"{x}, {y}");
            var color = texture.GetPixel(x, y);
            texture.Apply();
            RenderTexture.active = currentActiveRT;
            return color;
        }

        public Component GetSelected(Vector2 position)
        {
            var color = ReadTexture((int)position.x, (int)position.y);
            Debug.Log(color);
            var id = ColorToId(color);
            if (idMap.ContainsKey(id))
            {
                return idMap[id];
            }
            else
            {
                Debug.LogError("ID not found");
                return null;
            }
        }

        public int Register(Component obj, bool includeChildren = true)
        {
            if (objMap.ContainsKey(obj.gameObject))
            {
                Debug.LogError("Object has already registered");
                return -1;
            }

            MeshRenderer[] renderers;
            if (includeChildren)
                renderers = obj.GetComponentsInChildren<MeshRenderer>();
            else
                renderers = obj.GetComponents<MeshRenderer>();

            if (renderers.Length == 0)
            {
                Debug.LogError("Object has no mesh renderer");
                return -1;
            }
            var id = AllocateId();
            var color = IdToColor(id);
            Debug.Log(color);
            mpb ??= new();
            mpb.SetVector("_IdColor", color);
            foreach (var r in renderers)
            {
                r.SetPropertyBlock(mpb);
                Debug.Log("Set Color");
            }

            idMap.Add(id, obj);
            objMap.Add(obj.gameObject, id);

            return id;
        }

        public void LogOff(Component obj)
        {
            if (!objMap.ContainsKey(obj.gameObject))
            {
                Debug.LogError("Object has not registered");
                return;
            }
            var id = objMap[obj.gameObject];
            idMap.Remove(id);
            objMap.Remove(obj.gameObject);
        }
    }
}