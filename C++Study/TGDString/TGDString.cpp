#include "TGDString.h"
namespace TGD
{
    TGDString::TGDString() : ptr(nullptr), size(0), reserved(0)
    {
        DefaultAllocate();
    }

    TGDString::~TGDString()
    {}

    TGDString::TGDString(const TGDString& string)
    {
        ReallocateGrowby(string.Length());

        ::memcpy(ptr, string.ptr, string.Length());
        size = string.size;
        reserved = string.reserved;
    }

    TGDString::TGDString(TGDString&& string) noexcept{
        ptr = string.ptr;
        size = string.size;
        reserved = string.reserved;

        string.ptr = nullptr;
        string.size = 0;
        string.reserved = 0;
    }

    void TGDString::Append(const char * chars)
    {
        const size_t strLength = strlen(chars);
        Append(chars, strLength);
    }

    void TGDString::Append(const TGDString & str)
    {
        Append(str.Cstr(), str.Length());
    }

    const size_t TGDString::Length() const noexcept {
        return size;
    }

    const char* TGDString::Cstr() const noexcept{
        return ptr;
    }

    void TGDString::Append(const char* chars, const size_t appendSize)
    {
        ReallocateGrowby(appendSize);
    }

    void TGDString::ReallocateGrowby(const size_t increaseSize)
    {
        const size_t newSize = size + increaseSize;

        // 예약된 사이즈보다 크다면 새로 할당합니다. 
        if( newSize > reserved )
        {
            const size_t newReallocate = GetGrowth(newSize);
            Allocate(newReallocate, newSize);
        }
    }

    size_t TGDString::GetGrowth(size_t newSize)
    {
        auto newAllocSize = newSize + DEFAULT_SIZE;
        if(newAllocSize > MaxSize())
        {
            return MaxSize();
        }
        return std::max(newAllocSize, reserved + (reserved / 2) );
    }

   
    void TGDString::DefaultAllocate()
    {
        Allocate(DEFAULT_SIZE, 0);
    }

    void TGDString::Allocate(const size_t allocSize, const size_t stringSize)
    {
        if (ptr)
            delete[] ptr;

        ptr = new char[allocSize];
        size = stringSize;
        reserved = allocSize;
    }
}