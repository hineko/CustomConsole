using System;
using System.Reflection;
using UnityEngine;

namespace CustomConsole.Editor
{
	[Serializable]
	public class SplitterState
	{
		public int currentActiveSplitter = -1;
		public int lastTotalSize = 0;
		private const int defaultSplitSize = 6;
		public int ID;
		public int splitterInitialOffset;
		public int[] realSizes;
		public float[] relativeSizes;
		public int[] minSizes;
		public int[] maxSizes;
		public int splitSize;
		public float xOffset;

		private object original;
		private static Type splitterState;

		public static Type GetSplitterState
		{
			get
			{
				return splitterState ?? (splitterState = Assembly.Load(CustomConsoleWindow.ASSEMBLY_NAME).GetType("UnityEditor.SplitterState"));
			}
		}

		public object GetOriginalState()
		{
			return original;
		}

		public SplitterState(params float[] relativeSizes)
		{
			Init(relativeSizes, (int[])null, (int[])null, 0);
		}

		public SplitterState(int[] realSizes, int[] minSizes, int[] maxSizes)
		{
			this.realSizes = realSizes;
			this.minSizes = minSizes != null ? minSizes : new int[realSizes.Length];
			this.maxSizes = maxSizes != null ? maxSizes : new int[realSizes.Length];
			relativeSizes = new float[realSizes.Length];
			splitSize = splitSize != 0 ? splitSize : 6;
			RealToRelativeSizes();
		}

		public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
		{
			Init(relativeSizes, minSizes, maxSizes, 0);
		}

		public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes, int splitSize)
		{
			Init(relativeSizes, minSizes, maxSizes, splitSize);
		}

		private void Init(float[] relativeSizes, int[] minSizes, int[] maxSizes, int splitSize)
		{
			this.relativeSizes = relativeSizes;
			this.minSizes = minSizes != null ? minSizes : new int[relativeSizes.Length];
			this.maxSizes = maxSizes != null ? maxSizes : new int[relativeSizes.Length];
			realSizes = new int[relativeSizes.Length];
			this.splitSize = splitSize != 0 ? splitSize : 6;
			NormalizeRelativeSizes();

			ConstructorInfo ctor = GetSplitterState.GetConstructor(new Type[]
				{typeof(float[]), typeof(int[]), typeof(int[]), typeof(int)});
			original = ctor.Invoke(new object[4] { relativeSizes, minSizes, maxSizes, splitSize });
		}

		public void NormalizeRelativeSizes()
		{
			float num1 = 1f;
			float num2 = 0.0f;
			for (int index = 0; index < relativeSizes.Length; ++index)
				num2 += relativeSizes[index];
			for (int index = 0; index < relativeSizes.Length; ++index)
			{
				relativeSizes[index] = relativeSizes[index] / num2;
				num1 -= relativeSizes[index];
			}

			relativeSizes[relativeSizes.Length - 1] += num1;
		}

		public void RealToRelativeSizes()
		{
			float num1 = 1f;
			float num2 = 0.0f;
			for (int index = 0; index < realSizes.Length; ++index)
				num2 += (float)realSizes[index];
			for (int index = 0; index < realSizes.Length; ++index)
			{
				relativeSizes[index] = (float)realSizes[index] / num2;
				num1 -= relativeSizes[index];
			}

			if (relativeSizes.Length <= 0)
				return;
			relativeSizes[relativeSizes.Length - 1] += num1;
		}

		public void RelativeToRealSizes(int totalSpace)
		{
			int num1 = totalSpace;
			for (int index = 0; index < this.relativeSizes.Length; ++index)
			{
				this.realSizes[index] = (int)Mathf.Round(this.relativeSizes[index] * (float)totalSpace);
				if (this.realSizes[index] < this.minSizes[index])
					this.realSizes[index] = this.minSizes[index];
				num1 -= this.realSizes[index];
			}

			if (num1 < 0)
			{
				for (int index = 0; index < this.relativeSizes.Length; ++index)
				{
					if (this.realSizes[index] > this.minSizes[index])
					{
						int num2 = this.realSizes[index] - this.minSizes[index];
						int num3 = -num1 < num2 ? -num1 : num2;
						num1 += num3;
						this.realSizes[index] -= num3;
						if (num1 >= 0)
							break;
					}
				}
			}

			int index1 = this.realSizes.Length - 1;
			if (index1 < 0)
				return;
			this.realSizes[index1] += num1;
			if (this.realSizes[index1] < this.minSizes[index1])
				this.realSizes[index1] = this.minSizes[index1];
		}

		public void DoSplitter(int i1, int i2, int diff)
		{
			int realSize1 = this.realSizes[i1];
			int realSize2 = this.realSizes[i2];
			int num1 = this.minSizes[i1];
			int num2 = this.minSizes[i2];
			int maxSize1 = this.maxSizes[i1];
			int maxSize2 = this.maxSizes[i2];
			bool flag = false;
			if (num1 == 0)
				num1 = 16;
			if (num2 == 0)
				num2 = 16;
			if (realSize1 + diff < num1)
			{
				diff -= num1 - realSize1;
				this.realSizes[i2] += this.realSizes[i1] - num1;
				this.realSizes[i1] = num1;
				if ((uint)i1 > 0U)
					this.DoSplitter(i1 - 1, i2, diff);
				else
					this.splitterInitialOffset -= diff;
				flag = true;
			}
			else if (realSize2 - diff < num2)
			{
				diff -= realSize2 - num2;
				this.realSizes[i1] += this.realSizes[i2] - num2;
				this.realSizes[i2] = num2;
				if (i2 != this.realSizes.Length - 1)
					this.DoSplitter(i1, i2 + 1, diff);
				else
					this.splitterInitialOffset -= diff;
				flag = true;
			}

			if (!flag)
			{
				if (maxSize1 != 0 && realSize1 + diff > maxSize1)
				{
					diff -= this.realSizes[i1] - maxSize1;
					this.realSizes[i2] += this.realSizes[i1] - maxSize1;
					this.realSizes[i1] = maxSize1;
					if ((uint)i1 > 0U)
						this.DoSplitter(i1 - 1, i2, diff);
					else
						this.splitterInitialOffset -= diff;
					flag = true;
				}
				else if (maxSize2 != 0 && realSize2 - diff > maxSize2)
				{
					diff -= realSize2 - maxSize2;
					this.realSizes[i1] += this.realSizes[i2] - maxSize2;
					this.realSizes[i2] = maxSize2;
					if (i2 != this.realSizes.Length - 1)
						this.DoSplitter(i1, i2 + 1, diff);
					else
						this.splitterInitialOffset -= diff;
					flag = true;
				}
			}

			if (flag)
				return;
			this.realSizes[i1] += diff;
			this.realSizes[i2] -= diff;
		}
	}
}