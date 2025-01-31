#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float Frequency;
    float Phase;
    float Variation;
    float3 AmountDistribution;
    float UseCurlNoise;
    float AmountFromVelocity;
}

RWStructuredBuffer<Particle> Particles : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint maxParticleCount, _;
    Particles.GetDimensions(maxParticleCount, _);
    if(i.x >= maxParticleCount) {
        return;
    }

    float3 variationOffset = hash41u(i.x).xyz * Variation;    
    float3 pos = Particles[i.x].p.position*0.9; // avoid simplex noice glitch at -1,0,0 
    float3 noiseLookup = (pos + variationOffset + Phase* float3(1,-1,0)  ) * Frequency;
    float3 velocity = Particles[i.x].velocity;
    float speed = length(velocity);

    Particles[i.x].velocity = velocity + (UseCurlNoise < 0.5 
        ? snoiseVec3(noiseLookup) * (Amount/100 + speed * AmountFromVelocity / 100 ) * AmountDistribution
        : curlNoise(noiseLookup) * (Amount/100 + speed * AmountFromVelocity / 100) * AmountDistribution);
}

