#include <algorithm>
#include "TGDString.h"
namespace TGD
{
    TGDString::TGDString() : ptr(nullptr), size(0), reserved(0)
    {
        DefaultAllocate();
    }

    TGDString::~TGDString()
    {}

    TGDString::TGDString(const char* chars) : ptr(nullptr), size(0), reserved(0) {
        Append(chars);
    }

    TGDString::TGDString(const TGDString& string)
    {
        
        Append(string.Cstr());
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

    void TGDString::Reserve(size_t reserved)
    {
        ReallocateGrowby(reserved);
    }


    const size_t TGDString::Length() const noexcept 
    {
        return size;
    }

    const char* TGDString::Cstr() const noexcept
    {
        if (!ptr)
            return "";
        return ptr;
    }

    const bool TGDString::Empty() const noexcept
    {
        return size == 0;
    }

    const int TGDString::Compare(const char* chars) const noexcept 
    {
        const size_t strLength = strlen(chars);
        if (strLength != size) { return false; }

        for (int i = 0; i < size; i++) {
            if (ptr[i] != chars[i])
            {
                return ptr[i] < chars[i] ? -1 : 1;
            }
        }
        return 0;
    }

    void TGDString::Append(const char* chars, const size_t appendSize)
    {
        const size_t newSize = size + appendSize;
        ReallocateGrowby(newSize);

        ::memcpy(ptr + size, chars, appendSize);
        size += appendSize;
    }

    void TGDString::ReallocateGrowby(const size_t newSize)
    {
        // 예약된 사이즈보다 크다면 새로 할당합니다. 
        if( newSize >= reserved )
        {
            const size_t newReallocate = GetGrowth(newSize);
            Allocate(newReallocate, size);
        }
    }

    size_t TGDString::GetGrowth(size_t newSize)
    {
        auto newAllocSize = newSize | DEFAULT_SIZE;
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
        // New Allocate 
        auto newPtr = new char[allocSize];
        ::memset(newPtr, 0, allocSize);

        if (ptr)
        {
            // OldPtr -> NewPtr memory copy
            ::memcpy(newPtr, ptr, size);
            delete[] ptr;
        }
        ptr = newPtr;

        size = stringSize;
        reserved = allocSize;
    }
}