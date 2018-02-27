using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace Intel.RealSense
{
    public class Points : Frame
    {
        public Points(IntPtr ptr) : base(ptr)
        {
        }
        public int Size
        {
            get
            {
                object error;
                return NativeMethods.rs2_get_frame_points_count(m_instance.Handle, out error);
            }
        }
        public IntPtr Vertices
        {
            get
            {
                object error;
                return NativeMethods.rs2_get_frame_vertices(m_instance.Handle, out error);
            }
        }
        public IntPtr TextureCoordinates
        {
            get
            {
                object error;
                return NativeMethods.rs2_get_frame_texture_coordinates(m_instance.Handle, out error);
            }
        }
    }
    public class PointCloud : ProcessingBlock
    {
        public PointCloud()
        {
            object error;
            m_instance = new HandleRef(this, NativeMethods.rs2_create_pointcloud(out error));
            queue = new FrameQueue();
            NativeMethods.rs2_start_processing_queue(m_instance.Handle, queue.m_instance.Handle, out error);
        }
        public Points Calclate(DepthFrame original)
        {
            object error;
            NativeMethods.rs2_frame_add_ref(original.m_instance.Handle, out error);
            NativeMethods.rs2_process_frame(m_instance.Handle, original.m_instance.Handle, out error);
            return queue.WaitForFrame() as Points;
        }
        public void MapTo(Frame mapped)
        {
            this.Options.Where(s => s.Key == Option.TextureSource).First().Value = mapped.Profile.UniqueID;
            object error;
            NativeMethods.rs2_frame_add_ref(mapped.m_instance.Handle, out error);
            NativeMethods.rs2_process_frame(m_instance.Handle, mapped.m_instance.Handle, out error);
        }

        FrameQueue queue;
    }
}
