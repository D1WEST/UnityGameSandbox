#include "NoiseCommon.hlsl"

#define K_VOR 0.142857142857
#define Ko_VOR 0.428571428571

float3 Permutation_Vor(float3 x)
{
    return mod289((34.0 * x + 1.0) * x);
}

float2 inoise(float3 P, float jitter)
{
    float3 Pi = mod289(floor(P));
    float3 Pf = frac(P);
    float3 oi = float3(-1.0, 0.0, 1.0);
    float3 of = float3(-0.5, 0.5, 1.5);
    float3 px = Permutation_Vor(Pi.x + oi);
    float3 py = Permutation_Vor(Pi.y + oi);
    float3 p, ox, oy, oz, dx, dy, dz;
    float2 F = 1e6;
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            p = Permutation_Vor(px[i] + py[j] + Pi.z + oi);
            ox = frac(p * K_VOR) - Ko_VOR;
            oy = mod289(floor(p * K_VOR) / 7.0).x * 7.0 * K_VOR - Ko_VOR; // упрощенно
            p = Permutation_Vor(p);
            oz = frac(p * K_VOR) - Ko_VOR;
            dx = Pf.x - of[i] + jitter * ox;
            dy = Pf.y - of[j] + jitter * oy;
            dz = Pf.z - of + jitter * oz;
            float3 d = dx * dx + dy * dy + dz * dz;
            for (int n = 0; n < 3; n++)
            {
                if (d[n] < F[0])
                {
                    F[1] = F[0];
                    F[0] = d[n];
                }
                else if (d[n] < F[1])
                {
                    F[1] = d[n];
                }
            }
        }
    }
    return F;
}