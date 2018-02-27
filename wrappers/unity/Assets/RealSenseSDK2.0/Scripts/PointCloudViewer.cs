using UnityEngine;
using System.Linq;
using System.Threading;
using Intel.RealSense;

public class PointCloudViewer : MonoBehaviour
{
    struct Buffer
    {
        public Vector3[] vertices;
        public Vector2[] texcoords;
    }

    public Material material;

    AutoResetEvent f = new AutoResetEvent(false);
    PointCloud pointCloud = new PointCloud();
    Texture2D texture;
    Mesh[] meshes;
    byte[] image;
    Buffer[] buffers;

    void Start ()
    {
        var colorProfile = RealSenseDevice.Instance.ActiveProfile.Streams.First(p => p.Stream == Stream.Color) as VideoStreamProfile;
        if (colorProfile == null) return;

        texture = new Texture2D(colorProfile.Width, colorProfile.Height, TextureFormat.RGB24, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        if (material == null)
        {
            material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = texture;
        }
        RealSenseDevice.Instance.onNewSampleSet += OnFrameSet;
    }
    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "RtlCopyMemory")]
    static unsafe extern void NativeCopyMemory(void* Destination, void* Source, uint Length);

    private unsafe void OnFrameSet(FrameSet frames)
    {
        var points = pointCloud.Calclate(frames.DepthFrame);
        var pointCount = points.Size;
        var color = frames.ColorFrame;
        pointCloud.MapTo(color);

        if (buffers == null)
        {
            image = new byte[color.Stride * color.Height];


            var meshCount = 1;
            while (pointCount / meshCount > 65536)
            {
                meshCount *= 2;
            }
            pointCount /= meshCount;
            buffers = new Buffer[meshCount];
            for (int i = 0; i < meshCount; ++i)
            {
                buffers[i].vertices = new Vector3[pointCount];
                buffers[i].texcoords = new Vector2[pointCount];
            }
        }
        else
        {
            pointCount /= buffers.Length;
        }

        color.CopyTo(image);
        Vector3* vs = (Vector3*)points.Vertices;
        Vector2* ts = (Vector2*)points.TextureCoordinates;
        for (int i = 0; i < buffers.Length; ++i)
        {
            fixed (Vector3* vd = &buffers[i].vertices[0])
            fixed (Vector2* td = &buffers[i].texcoords[0])
            {
                NativeCopyMemory(vd, vs, (uint)(sizeof(Vector3) * pointCount));
                NativeCopyMemory(td, ts, (uint)(sizeof(Vector2) * pointCount));
                vs += pointCount;
                ts += pointCount;
            }
        }
        f.Set();
    }
    void Update()
    {
        if (f.WaitOne(0))
        {
            if (meshes == null)
            {
                var indices = Enumerable.Range(0, buffers[0].vertices.Length).ToArray();
                meshes = new Mesh[buffers.Length];
                for (int i = 0; i < buffers.Length; ++i)
                {
                    meshes[i] = new Mesh();
                    meshes[i].vertices = buffers[i].vertices;
                    meshes[i].uv = buffers[i].texcoords;
                    meshes[i].SetIndices(indices, MeshTopology.Points, 0);

                    var obj = new GameObject("mesh" + i);
                    obj.transform.SetParent(this.transform, false);
                    var filter = obj.AddComponent<MeshFilter>();
                    filter.sharedMesh = meshes[i];
                    var renderer = obj.AddComponent<MeshRenderer>();
                    renderer.material = material;
                }
            }
            else
            {
                for (int i = 0; i < buffers.Length; ++i)
                {
                    meshes[i].vertices = buffers[i].vertices;
                    meshes[i].uv = buffers[i].texcoords;
                }
            }
            texture.LoadRawTextureData(image);
            texture.Apply();
        }
    }
}
