#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float SmoothDistance;
    float SampleMode;
    float2 SampleRange;
}

StructuredBuffer<Point> SourcePoints : t0;         // input
RWStructuredBuffer<Point> ResultPoints : u0;    // output


static uint sourceCount;
static float3 sumPos =0;
static float sumWeight=0;
static int sampledCount=0;
void SamplePosAtF(float f) 
{
    float sourceF = saturate(f) * (sourceCount -1);
    uint index = (int)sourceF;
    if(index > sourceCount -2)
        return;

    float w1= SourcePoints[index].w;
    if(isnan(w1)) {
        return;
    }

    float w2= SourcePoints[index+1].w; 
    if(isnan(w2)) {
        return;
    }

    float fraction = sourceF - index;    
    sumWeight += lerp(w1, w2, fraction );
    sumPos += lerp(SourcePoints[index].position, SourcePoints[index+1].position , fraction );
    sampledCount++;
}

float4 SampleRotationAtF(float f) 
{
    float sourceF = saturate(f) * (sourceCount -1);
    int index = (int)sourceF;
    float fraction = sourceF - index;    
    index = clamp(index,0, sourceCount -1);
    return q_slerp(SourcePoints[index].rotation, SourcePoints[index+1].rotation, fraction );
}




[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount, stride;
    ResultPoints.GetDimensions(pointCount, stride);

    if(i.x >= pointCount) {
        return;
    }

    uint stride2;
    SourcePoints.GetDimensions(sourceCount, stride);

    float fNormlized = (float)i.x/pointCount;
    
    float rightFactor = SampleMode > 0.5 ? SampleRange.x : 0;
    float f = SampleRange.x + fNormlized * (SampleRange.y - rightFactor);

    if(f <0 || f >= 1) {
        ResultPoints[i.x].w = sqrt(-1);
        return;
    }

    int maxSteps = 5;
    //float sampledWeight;
    sumWeight = 0;
    sampledCount = 0;
    //float3 sumPoint;
    SamplePosAtF( f);
    //sumWeight += sampledWeight;
    
    for(int step = 1; step <= maxSteps; step++) 
    {
        float d = step * SmoothDistance / maxSteps / sourceCount;
        
        SamplePosAtF( f - d);
        //sumWeight += sampledWeight;
        SamplePosAtF( f + d);
        //sumWeight += sampledWeight;
    }

    //sumPos /= (maxSteps * 2 + 1);
    sumPos /= (sampledCount);
    
    //sumWeight /=(maxSteps * 2 + 1);
    sumWeight /= sampledCount;
    
    if(sampledCount==0)
       sumWeight = sqrt(-1);

    ResultPoints[i.x].position = sumPos;
    ResultPoints[i.x].w = sumWeight;

    ResultPoints[i.x].rotation = SampleRotationAtF(f);// float4(0,0,0,1);//  p.rotation; //qmul(rotationFromDisplace , SourcePoints[i.x].rotation);
    //ResultPoints[i.x].w = 1;//SourcePoints[i.x].w;
}

