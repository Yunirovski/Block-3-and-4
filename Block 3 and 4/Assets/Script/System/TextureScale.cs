// Assets/Scripts/Utils/TextureScale.cs
// �򻯰棬ֻ���� Bilinear �� Point �������㹻������ͼ
using System.Threading;
using UnityEngine;

public class TextureScale
{
    public class ThreadData
    {
        public int start;
        public int end;
        public ThreadData(int s, int e) { start = s; end = e; }
    }

    private static Color[] texColors;
    private static Color[] newColors;
    private static int w;
    private static float ratioX;
    private static float ratioY;
    private static int w2;
    private static int finishCount;
    private static Mutex mutex;

    public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
    {
        ThreadedScale(tex, newWidth, newHeight, true);
    }

    public static void Point(Texture2D tex, int newWidth, int newHeight)
    {
        ThreadedScale(tex, newWidth, newHeight, false);
    }

    private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight, bool useBilinear)
    {
        texColors = tex.GetPixels();
        newColors = new Color[newWidth * newHeight];
        if (useBilinear)
        {
            ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
            ratioY = 1.0f / ((float)newHeight / (tex.height - 1));
        }
        else
        {
            ratioX = (float)tex.width / newWidth;
            ratioY = (float)tex.height / newHeight;
        }
        w = tex.width;
        w2 = newWidth;

        int cores = Mathf.Min(SystemInfo.processorCount, newHeight);
        int slice = newHeight / cores;

        finishCount = 0;
        if (mutex == null) mutex = new Mutex(false);

        if (cores > 1)
        {
            for (int i = 0; i < cores - 1; i++)
            {
                ThreadData threadData = new ThreadData(slice * i, slice * (i + 1));
                ParameterizedThreadStart ts = useBilinear
                    ? new ParameterizedThreadStart(BilinearScale)
                    : new ParameterizedThreadStart(PointScale);
                Thread thread = new Thread(ts);
                thread.Start(threadData);
            }
            ThreadData threadDataMain = new ThreadData(slice * (cores - 1), newHeight);
            if (useBilinear)
                BilinearScale(threadDataMain);
            else
                PointScale(threadDataMain);

            while (finishCount < cores) Thread.Sleep(1);
        }
        else
        {
            ThreadData threadData = new ThreadData(0, newHeight);
            if (useBilinear)
                BilinearScale(threadData);
            else
                PointScale(threadData);
        }

        tex.Reinitialize(newWidth, newHeight, TextureFormat.RGB24, false);
        tex.SetPixels(newColors);
        tex.Apply();
    }

    private static void BilinearScale(object obj)
    {
        ThreadData threadData = (ThreadData)obj;
        for (var y = threadData.start; y < threadData.end; y++)
        {
            int yFloor = (int)Mathf.Floor(y * ratioY);
            var y1 = yFloor * w;
            var y2 = (yFloor + 1) * w;
            var yw = y * w2;

            for (var x = 0; x < w2; x++)
            {
                int xFloor = (int)Mathf.Floor(x * ratioX);
                var xLerp = x * ratioX - xFloor;

                Color top = Color.Lerp(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp);
                Color bottom = Color.Lerp(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp);
                newColors[yw + x] = Color.Lerp(top, bottom, y * ratioY - yFloor);
            }
        }

        mutex.WaitOne();
        finishCount++;
        mutex.ReleaseMutex();
    }

    private static void PointScale(object obj)
    {
        ThreadData threadData = (ThreadData)obj;
        for (var y = threadData.start; y < threadData.end; y++)
        {
            var thisY = (int)(ratioY * y) * w;
            var yw = y * w2;
            for (var x = 0; x < w2; x++)
                newColors[yw + x] = texColors[thisY + (int)(ratioX * x)];
        }

        mutex.WaitOne();
        finishCount++;
        mutex.ReleaseMutex();
    }
}
