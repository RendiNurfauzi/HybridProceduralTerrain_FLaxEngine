using System;
using FlaxEngine;

public static class WorldHydrology
{
    public static void Erode(float[] map, int size, int droplets, float erosionRate, float depositionRate, float gravity)
    {
        Random rnd = new Random();

        for (int i = 0; i < droplets; i++)
        {
            float px = (float)rnd.NextDouble() * (size - 1.1f);
            float py = (float)rnd.NextDouble() * (size - 1.1f);

            float dirX = 0, dirY = 0;
            float sediment = 0;
            float water = 1;
            float velocity = 1;

            for (int step = 0; step < 35; step++)
            {
                int ix = (int)px;
                int iy = (int)py;
                int idx = iy * size + ix;

                float gx = map[idx + 1] - map[idx];
                float gy = map[idx + size] - map[idx];

                dirX = dirX * 0.1f - gx;
                dirY = dirY * 0.1f - gy;

                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len > 0)
                {
                    dirX /= len;
                    dirY /= len;
                }

                px += dirX;
                py += dirY;

                if (px < 0 || px >= size - 1 || py < 0 || py >= size - 1)
                    break;

                float newHeight = map[(int)py * size + (int)px];
                float delta = map[idx] - newHeight;

                float capacity = Mathf.Max(delta, 0.01f) * velocity * water * 5f;

                if (sediment > capacity)
                {
                    float deposit = (sediment - capacity) * depositionRate;
                    sediment -= deposit;
                    map[idx] += deposit;
                }
                else
                {
                    float erode = Mathf.Min((capacity - sediment) * erosionRate, delta);
                    sediment += erode;
                    map[idx] -= erode;
                }

                velocity = Mathf.Sqrt(Mathf.Max(0, velocity * velocity + delta * gravity));
                water *= 0.98f;
            }
        }
    }

    public static void ApplyThermal(float[] map, int size, float strength, float angleThreshold)
    {
        for (int iter = 0; iter < 2; iter++)
        {
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    int idx = y * size + x;
                    int[] neighbors =
                    {
                        idx + 1,
                        idx - 1,
                        idx + size,
                        idx - size
                    };

                    foreach (int n in neighbors)
                    {
                        float diff = map[idx] - map[n];
                        if (diff > angleThreshold / 1000f)
                        {
                            float move = (diff - angleThreshold / 1000f) * strength;
                            map[idx] -= move;
                            map[n] += move;
                        }
                    }
                }
            }
        }
    }
}