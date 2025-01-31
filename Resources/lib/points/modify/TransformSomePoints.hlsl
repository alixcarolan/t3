#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformMatrix;

    float UpdateRotation;   // 16
    float ScaleW;
    float OffsetW;
    float CoordinateSpace;

    float WIsWeight;        // 20
    float RangeStart;
    float RangeLength;
    float Take;

    float Skip;             // 24
    float Scatter;

    float OnlyKeepTakes;
}


StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   

static const float PointSpace = 0;
static const float ObjectSpace = 1;
static const float WorldSpace = 2;

uint imod(uint x, uint y) {
    return (x - y * floor(x / y));
} 


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint sourceCount, resultCount, stride;
    SourcePoints.GetDimensions(sourceCount, stride);
    ResultPoints.GetDimensions(resultCount, stride);

    if(i.x >= resultCount) {
        return;
    }

    uint segmentCount = sourceCount ;    // Number of lines between points

    uint iTake = (int)Take;
    uint iSkip = (int)Skip;
    uint iGroupSize = iTake + iSkip;
    uint sourceIndex=i.x;
    uint resultIndex = i.x;

    float theta = 0.0001;
    
    if(OnlyKeepTakes > 0.5) 
    {
        int sourceStartIndex = RangeStart * sourceCount + 1  - theta;

        int iGroupIndex = i.x / iTake;
        int indexInGroup = i.x % iTake;
        int offset = (iGroupIndex * (iTake+iSkip) + indexInGroup + sourceStartIndex);
        sourceIndex = (offset + sourceCount * 1000000) % sourceCount;
    }
    else 
    {        
        float f= mod((float)i.x / segmentCount - RangeStart ,1 + theta);
        uint indexInRange = f * segmentCount;
        uint groupIndex = (indexInRange) / iGroupSize;
        uint indexInGroup = indexInRange % iGroupSize;
        ResultPoints[resultIndex] = SourcePoints[sourceIndex];

        // Copy points outside of range
        if(indexInRange  < 0 || indexInRange > (uint)(RangeLength * segmentCount) || indexInGroup >= iTake) 
        {
            return;
        }

        ResultPoints[resultIndex].w = indexInGroup;
    }
                    
    float w = SourcePoints[sourceIndex].w;
    float3 pOrg = SourcePoints[sourceIndex].position;
    float3 p = pOrg;

    float4 orgRot = SourcePoints[sourceIndex].rotation;
    float4 rotation = orgRot;

    if(CoordinateSpace < 0.5) {
        p.xyz = 0;
        rotation = float4(0,0,0,1);
    }
 
    float3 pLocal = p;
    p = mul(float4(p,1), TransformMatrix).xyz;

    float4 newRotation = rotation;

    // Transform rotation is kind of tricky. There might be more efficient ways to do this.
    if(UpdateRotation > 0.5) 
    {
        float3x3 orientationDest = float3x3(
            TransformMatrix._m00_m01_m02,
            TransformMatrix._m10_m11_m12,
            TransformMatrix._m20_m21_m22);


        newRotation = normalize(quaternion_from_matrix_precise(transpose(orientationDest)));        

        // Adjust rotation in point space
        if(CoordinateSpace  < 0.5) {
            newRotation = qmul(orgRot, newRotation);
        }
        else {
            newRotation = qmul(newRotation, orgRot);
        }
    }

    if(WIsWeight >= 0.5) {
        float3 weightedOffset = (p - pLocal) * w;
        p = pLocal + weightedOffset;

        newRotation = q_slerp(orgRot, newRotation, w);
    }

    if(CoordinateSpace < 0.5) {     
        p.xyz = rotate_vector(p.xyz, orgRot).xyz;
        p += pOrg;
    } 

    ResultPoints[resultIndex].position = p.xyz;
    ResultPoints[resultIndex].rotation = newRotation;
    ResultPoints[resultIndex].w = w * ScaleW + OffsetW;    
}

