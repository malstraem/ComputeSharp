using System;
using ComputeSharp.Tests.Attributes;
using ComputeSharp.Tests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ComputeSharp.Tests
{
    [TestClass]
    [TestCategory("Dispatch")]
    public partial class DispatchTests
    {
        [CombinatorialTestMethod]
        [AllDevices]
        public void Verify_ThreadIds(Device device)
        {
            using ReadWriteTexture3D<Int4> buffer = device.Get().AllocateReadWriteTexture3D<Int4>(50, 50, 50);

            device.Get().For(buffer.Width, buffer.Height, buffer.Depth, new ThreadIdsShader(buffer));

            Int4[,,] data = buffer.ToArray();

            for (int z = 0; z < 50; z++)
            {
                for (int x = 0; x < 50; x++)
                {
                    for (int y = 0; y < 50; y++)
                    {
                        Int4 value = data[z, y, x];

                        Assert.AreEqual(x, value.X);
                        Assert.AreEqual(y, value.Y);
                        Assert.AreEqual(z, value.Z);
                    }
                }
            }
        }

        [CombinatorialTestMethod]
        [AllDevices]
        [Data(0, 8, 8)]
        [Data(8, 0, 8)]
        [Data(8, 8, 0)]
        [Data(8, -3, 16)]
        [Data(-1, -1, -1)]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Verify_ThreadIds_OutOfRange(Device device, int x, int y, int z)
        {
            using ReadWriteTexture3D<Int4> buffer = device.Get().AllocateReadWriteTexture3D<Int4>(50, 50, 50);

            device.Get().For(x, y, z, new ThreadIdsShader(buffer));

            Assert.Fail();
        }

        [AutoConstructor]
        internal readonly partial struct ThreadIdsShader : IComputeShader
        {
            public readonly ReadWriteTexture3D<Int4> buffer;

            public void Execute()
            {
                buffer[ThreadIds.XYZ].XYZ = ThreadIds.XYZ;
            }
        }

        [CombinatorialTestMethod]
        [AllDevices]
        public void Verify_GroupIds(Device device)
        {
            using ReadWriteTexture3D<Int4> buffer = device.Get().AllocateReadWriteTexture3D<Int4>(50, 50, 50);

            device.Get().For(buffer.Width, buffer.Height, buffer.Depth, 4, 4, 4, new GroupIdsShader(buffer));

            Int4[,,] data = buffer.ToArray();

            for (int z = 0; z < 50; z++)
            {
                for (int x = 0; x < 50; x++)
                {
                    for (int y = 0; y < 50; y++)
                    {
                        Int4 value = data[z, y, x];

                        Assert.AreEqual(x % 4, value.X);
                        Assert.AreEqual(y % 4, value.Y);
                        Assert.AreEqual(z % 4, value.Z);
                        Assert.AreEqual(value.Z * 4 * 4 + value.Y * 4 + value.X, value.W);
                    }
                }
            }
        }

        [CombinatorialTestMethod]
        [AllDevices]
        [Data(0, 8, 8)]
        [Data(8, 0, 8)]
        [Data(8, 8, 0)]
        [Data(1025, 8, 8)]
        [Data(8, 2000, 8)]
        [Data(8, 8, 70)]
        [Data(8, -1, 8)]
        [Data(8, 15, 16)]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Verify_GroupIds_OutOfRange(Device device, int x, int y, int z)
        {
            using ReadWriteTexture3D<Int4> buffer = device.Get().AllocateReadWriteTexture3D<Int4>(50, 50, 50);

            device.Get().For(buffer.Width, buffer.Height, buffer.Depth, x, y, z, new GroupIdsShader(buffer));

            Assert.Fail();
        }

        [AutoConstructor]
        internal readonly partial struct GroupIdsShader : IComputeShader
        {
            public readonly ReadWriteTexture3D<Int4> buffer;

            public void Execute()
            {
                buffer[ThreadIds.XYZ].XYZ = GroupIds.XYZ;
                buffer[ThreadIds.XYZ].W = GroupIds.Index;
            }
        }

        [CombinatorialTestMethod]
        [AllDevices]
        public void Verify_GroupSize(Device device)
        {
            using ReadWriteBuffer<int> buffer = device.Get().AllocateReadWriteBuffer<int>(32);

            device.Get().For(1, 1, 1, 4, 15, 7, new GroupSizeShader(buffer));
            
            int[] data = buffer.ToArray();

            Assert.AreEqual(4, data[0]);
            Assert.AreEqual(15, data[1]);
            Assert.AreEqual(7, data[2]);
            Assert.AreEqual(4 * 15 * 7, data[3]);
        }

        [AutoConstructor]
        internal readonly partial struct GroupSizeShader : IComputeShader
        {
            public readonly ReadWriteBuffer<int> buffer;

            public void Execute()
            {
                buffer[0] = GroupSize.X;
                buffer[1] = GroupSize.Y;
                buffer[2] = GroupSize.Z;
                buffer[3] = GroupSize.Count;
            }
        }
    }
}
