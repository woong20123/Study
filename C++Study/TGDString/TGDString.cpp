#include "TGDString.h"
namespace TGD
{
    TGDString::TGDString() : ptr(nullptr), size(0), reserved(0)
    {}

    TGDString::~TGDString()
    {}


    // 버퍼 재할당
    void TGDString::ReallocateGrowby(const size_t increaseSize)
    {
        const size_t newSize = reserved + increaseSize;
    }

    size_t TGDString::GetGrowth(size_t increaseSize)
    {
        auto newAllocSize = increaseSize + DEFAULT_SIZE;
        if(newAllocSize > MaxSize())
        {
            return MaxSize();
        }
        return std::max(newAllocSize, reserved + reserved / 2);
    }

}